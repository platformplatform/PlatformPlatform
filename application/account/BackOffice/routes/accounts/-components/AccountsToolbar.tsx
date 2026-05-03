import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { InputGroup, InputGroupAddon, InputGroupButton, InputGroupInput } from "@repo/ui/components/InputGroup";
import { ToggleGroup, ToggleGroupItem } from "@repo/ui/components/ToggleGroup";
import { useDebounce } from "@repo/ui/hooks/useDebounce";
import { useNavigate } from "@tanstack/react-router";
import { SearchIcon, XIcon } from "lucide-react";
import { useEffect, useState } from "react";

import { SubscriptionPlan, TenantStatusFilter } from "@/shared/lib/api/client";
import { getSubscriptionPlanLabel } from "@/shared/lib/api/labels";

interface AccountsToolbarProps {
  search: string | undefined;
  plans: SubscriptionPlan[];
  statuses: TenantStatusFilter[];
}

export function AccountsToolbar({ search, plans, statuses }: Readonly<AccountsToolbarProps>) {
  const navigate = useNavigate();
  const [searchInput, setSearchInput] = useState(search ?? "");
  const debouncedSearch = useDebounce(searchInput, 500);

  useEffect(() => {
    if ((debouncedSearch || undefined) === search) {
      return;
    }
    navigate({
      to: "/accounts",
      search: (previous) => ({ ...previous, search: debouncedSearch || undefined, pageOffset: undefined })
    });
  }, [debouncedSearch, navigate, search]);

  useEffect(() => {
    setSearchInput(search ?? "");
  }, [search]);

  const handlePlansChange = (values: string[]) => {
    const next = values as SubscriptionPlan[];
    navigate({
      to: "/accounts",
      search: (previous) => ({
        ...previous,
        plans: next.length === 0 ? undefined : next,
        pageOffset: undefined
      })
    });
  };

  const handleStatusesChange = (values: string[]) => {
    const next = values as TenantStatusFilter[];
    navigate({
      to: "/accounts",
      search: (previous) => ({
        ...previous,
        statuses: next.length === 0 ? undefined : next,
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
            placeholder={t`Search by name`}
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
        aria-label={t`Plan`}
        multiple={true}
        value={plans}
        onValueChange={handlePlansChange}
      >
        {[SubscriptionPlan.Premium, SubscriptionPlan.Standard, SubscriptionPlan.Basis].map((value) => (
          <ToggleGroupItem key={value} value={value} className="min-w-[5rem] justify-center">
            {getSubscriptionPlanLabel(value)}
          </ToggleGroupItem>
        ))}
      </ToggleGroup>

      <ToggleGroup
        variant="outline"
        aria-label={t`Status`}
        multiple={true}
        value={statuses}
        onValueChange={handleStatusesChange}
      >
        <ToggleGroupItem value={TenantStatusFilter.Active} className="min-w-[5rem] justify-center">
          <Trans>Active</Trans>
        </ToggleGroupItem>
        <ToggleGroupItem value={TenantStatusFilter.Downgrading} className="min-w-[5rem] justify-center">
          <Trans>Downgrading</Trans>
        </ToggleGroupItem>
        <ToggleGroupItem value={TenantStatusFilter.Canceling} className="min-w-[5rem] justify-center">
          <Trans>Canceling</Trans>
        </ToggleGroupItem>
        <ToggleGroupItem value={TenantStatusFilter.Canceled} className="min-w-[5rem] justify-center">
          <Trans>Canceled</Trans>
        </ToggleGroupItem>
        <ToggleGroupItem value={TenantStatusFilter.Free} className="min-w-[5rem] justify-center">
          <Trans>Free</Trans>
        </ToggleGroupItem>
      </ToggleGroup>
    </div>
  );
}
