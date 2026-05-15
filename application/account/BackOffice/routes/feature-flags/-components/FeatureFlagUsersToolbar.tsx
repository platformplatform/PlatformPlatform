import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { InputGroup, InputGroupAddon, InputGroupButton, InputGroupInput } from "@repo/ui/components/InputGroup";
import { ToggleGroup, ToggleGroupItem } from "@repo/ui/components/ToggleGroup";
import { useDebounce } from "@repo/ui/hooks/useDebounce";
import { useNavigate } from "@tanstack/react-router";
import { SearchIcon, XIcon } from "lucide-react";
import { useEffect, useState } from "react";

import { FeatureFlagAudienceState, UserRole } from "@/shared/lib/api/client";
import { getUserRoleLabel } from "@/shared/lib/api/labels";

import type { StateFilter } from "./stateFilter";

import { ALL_STATE_FILTER, DEFAULT_STATE_FILTER } from "./stateFilter";

interface FeatureFlagUsersToolbarProps {
  flagKey: string;
  search: string | undefined;
  roles: UserRole[];
  state: StateFilter | undefined;
  hasOverride: boolean;
  hideHasOverride?: boolean;
  hideState?: boolean;
}

const HAS_OVERRIDE_VALUE = "true";

export function FeatureFlagUsersToolbar({
  flagKey,
  search,
  roles,
  state,
  hasOverride,
  hideHasOverride,
  hideState
}: Readonly<FeatureFlagUsersToolbarProps>) {
  const navigate = useNavigate();
  const [searchInput, setSearchInput] = useState(search ?? "");
  const debouncedSearch = useDebounce(searchInput, 500);

  useEffect(() => {
    if ((debouncedSearch || undefined) === search) {
      return;
    }
    navigate({
      to: "/feature-flags/$flagKey",
      params: { flagKey },
      search: (previous) => ({
        ...previous,
        usersSearch: debouncedSearch || undefined,
        usersPageOffset: undefined
      })
    });
  }, [debouncedSearch, navigate, flagKey, search]);

  useEffect(() => {
    setSearchInput(search ?? "");
  }, [search]);

  const handleStateChange = (values: string[]) => {
    // Single-select with multi-select "last item wins" — keep the most recently clicked chip pressed.
    // Clicking the already-pressed chip would otherwise deselect to []; default back to Enabled so one chip is always on.
    const next = (values[values.length - 1] as StateFilter | undefined) ?? DEFAULT_STATE_FILTER;
    navigate({
      to: "/feature-flags/$flagKey",
      params: { flagKey },
      search: (previous) => ({
        ...previous,
        usersState: next === DEFAULT_STATE_FILTER ? undefined : next,
        usersPageOffset: undefined
      })
    });
  };

  const handleHasOverrideChange = (values: string[]) => {
    navigate({
      to: "/feature-flags/$flagKey",
      params: { flagKey },
      search: (previous) => ({
        ...previous,
        usersHasOverride: values.length > 0 ? true : undefined,
        usersPageOffset: undefined
      })
    });
  };

  const handleRolesChange = (values: string[]) => {
    const next = values as UserRole[];
    navigate({
      to: "/feature-flags/$flagKey",
      params: { flagKey },
      search: (previous) => ({
        ...previous,
        usersRoles: next.length === 0 ? undefined : next,
        usersPageOffset: undefined
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
            placeholder={t`Search by name or email`}
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

      {!hideState && (
        <ToggleGroup
          variant="outline"
          aria-label={t`State`}
          multiple={true}
          value={[state ?? DEFAULT_STATE_FILTER]}
          onValueChange={handleStateChange}
        >
          <ToggleGroupItem value={ALL_STATE_FILTER} className="min-w-[5rem] justify-center">
            <Trans>All</Trans>
          </ToggleGroupItem>
          <ToggleGroupItem value={FeatureFlagAudienceState.Enabled} className="min-w-[5rem] justify-center">
            <Trans>Enabled</Trans>
          </ToggleGroupItem>
          <ToggleGroupItem value={FeatureFlagAudienceState.Disabled} className="min-w-[5rem] justify-center">
            <Trans>Disabled</Trans>
          </ToggleGroupItem>
        </ToggleGroup>
      )}

      {!hideHasOverride && (
        <ToggleGroup
          variant="outline"
          aria-label={t`Override`}
          multiple={true}
          value={hasOverride ? [HAS_OVERRIDE_VALUE] : []}
          onValueChange={handleHasOverrideChange}
        >
          <ToggleGroupItem value={HAS_OVERRIDE_VALUE} className="min-w-[5rem] justify-center">
            <Trans>Has override</Trans>
          </ToggleGroupItem>
        </ToggleGroup>
      )}

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
    </div>
  );
}
