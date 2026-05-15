import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { InputGroup, InputGroupAddon, InputGroupButton, InputGroupInput } from "@repo/ui/components/InputGroup";
import { ToggleGroup, ToggleGroupItem } from "@repo/ui/components/ToggleGroup";
import { useDebounce } from "@repo/ui/hooks/useDebounce";
import { useNavigate } from "@tanstack/react-router";
import { SearchIcon, XIcon } from "lucide-react";
import { useEffect, useState } from "react";

import { UserActivityFilter, UserRole } from "@/shared/lib/api/client";
import { getUserRoleLabel } from "@/shared/lib/api/labels";

interface UsersToolbarProps {
  search: string | undefined;
  roles: UserRole[];
  activity: UserActivityFilter | undefined;
}

export function UsersToolbar({ search, roles, activity }: Readonly<UsersToolbarProps>) {
  const navigate = useNavigate();
  const [searchInput, setSearchInput] = useState(search ?? "");
  const debouncedSearch = useDebounce(searchInput, 500);

  useEffect(() => {
    // `search` is intentionally NOT a dep here so external URL changes (Clear filters) don't fire
    // this effect with a stale debouncedSearch and immediately re-push the old typed value back
    // into the URL. The companion sync effect below handles URL → input. This effect only runs
    // when the user types something new and the debounce settles.
    if ((debouncedSearch || undefined) === search) return;
    navigate({
      to: "/users",
      search: (previous) => ({ ...previous, search: debouncedSearch || undefined, pageOffset: undefined })
    });
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [debouncedSearch, navigate]);

  useEffect(() => {
    setSearchInput(search ?? "");
  }, [search]);

  const handleRolesChange = (values: string[]) => {
    const next = values as UserRole[];
    navigate({
      to: "/users",
      search: (previous) => ({
        ...previous,
        roles: next.length === 0 ? undefined : next,
        pageOffset: undefined
      })
    });
  };

  // Activity is single-select on the backend; the toggle group exposes a multi-select array, so we keep only the
  // newly toggled value (last item in the array) and clear when the user deselects.
  const handleActivityChange = (values: string[]) => {
    const next = values.length === 0 ? undefined : (values[values.length - 1] as UserActivityFilter);
    navigate({
      to: "/users",
      search: (previous) => ({
        ...previous,
        activity: next,
        pageOffset: undefined
      })
    });
  };

  return (
    <div className="mb-4 flex flex-wrap items-center gap-3">
      <div className="max-w-[20rem] min-w-[14rem] flex-1">
        <InputGroup>
          <InputGroupAddon>
            <SearchIcon />
          </InputGroupAddon>
          <InputGroupInput
            type="text"
            role="searchbox"
            aria-label={t`Search`}
            placeholder={t`Search by email, name, or account`}
            value={searchInput}
            onChange={(event) => setSearchInput(event.target.value)}
            onKeyDown={(event) => event.key === "Escape" && searchInput && setSearchInput("")}
          />
          {searchInput && (
            <InputGroupAddon align="inline-end">
              <InputGroupButton onClick={() => setSearchInput("")} size="icon-xs" aria-label={t`Clear search`}>
                <XIcon />
              </InputGroupButton>
            </InputGroupAddon>
          )}
        </InputGroup>
      </div>

      <ToggleGroup
        variant="outline"
        aria-label={t`Role`}
        multiple={true}
        value={roles}
        onValueChange={handleRolesChange}
      >
        {[UserRole.Owner, UserRole.Admin, UserRole.Member].map((value) => (
          <ToggleGroupItem key={value} value={value} className="min-w-[5rem] justify-center">
            {getUserRoleLabel(value)}
          </ToggleGroupItem>
        ))}
      </ToggleGroup>

      <ToggleGroup
        variant="outline"
        aria-label={t`Activity`}
        value={activity ? [activity] : []}
        onValueChange={handleActivityChange}
      >
        <ToggleGroupItem value={UserActivityFilter.ActiveLast24Hours} className="min-w-[5rem] justify-center">
          <Trans>24h</Trans>
        </ToggleGroupItem>
        <ToggleGroupItem value={UserActivityFilter.ActiveLast7Days} className="min-w-[5rem] justify-center">
          <Trans>7 days</Trans>
        </ToggleGroupItem>
        <ToggleGroupItem value={UserActivityFilter.ActiveLast30Days} className="min-w-[5rem] justify-center">
          <Trans>30 days</Trans>
        </ToggleGroupItem>
        <ToggleGroupItem value={UserActivityFilter.InactiveOver30Days} className="min-w-[5rem] justify-center">
          <Trans>Inactive</Trans>
        </ToggleGroupItem>
      </ToggleGroup>
    </div>
  );
}
