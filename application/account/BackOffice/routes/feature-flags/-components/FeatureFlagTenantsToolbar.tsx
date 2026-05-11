import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { InputGroup, InputGroupAddon, InputGroupButton, InputGroupInput } from "@repo/ui/components/InputGroup";
import { ToggleGroup, ToggleGroupItem } from "@repo/ui/components/ToggleGroup";
import { useDebounce } from "@repo/ui/hooks/useDebounce";
import { useNavigate } from "@tanstack/react-router";
import { SearchIcon, XIcon } from "lucide-react";
import { useEffect, useState } from "react";

import { FeatureFlagAudienceState, SubscriptionPlan } from "@/shared/lib/api/client";
import { getSubscriptionPlanLabel } from "@/shared/lib/api/labels";

import type { StateFilter } from "./stateFilter";

import { DEFAULT_STATE_FILTER, fromToggleValues, toToggleValues } from "./stateFilter";

interface FeatureFlagTenantsToolbarProps {
  flagKey: string;
  search: string | undefined;
  plans: SubscriptionPlan[];
  state: StateFilter | undefined;
}

export function FeatureFlagTenantsToolbar({ flagKey, search, plans, state }: Readonly<FeatureFlagTenantsToolbarProps>) {
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
        tenantsSearch: debouncedSearch || undefined,
        tenantsPageOffset: undefined
      })
    });
  }, [debouncedSearch, navigate, flagKey, search]);

  useEffect(() => {
    setSearchInput(search ?? "");
  }, [search]);

  const handleStateChange = (values: string[]) => {
    const next = fromToggleValues(values);
    navigate({
      to: "/feature-flags/$flagKey",
      params: { flagKey },
      search: (previous) => ({
        ...previous,
        tenantsState: next === DEFAULT_STATE_FILTER ? undefined : next,
        tenantsPageOffset: undefined
      })
    });
  };

  const handlePlansChange = (values: string[]) => {
    const next = values as SubscriptionPlan[];
    navigate({
      to: "/feature-flags/$flagKey",
      params: { flagKey },
      search: (previous) => ({
        ...previous,
        tenantsPlans: next.length === 0 ? undefined : next,
        tenantsPageOffset: undefined
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
            placeholder={t`Search by account name or owner email`}
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
        aria-label={t`State`}
        multiple={true}
        value={toToggleValues(state)}
        onValueChange={handleStateChange}
      >
        <ToggleGroupItem value={FeatureFlagAudienceState.Enabled} className="min-w-[5rem] justify-center">
          <Trans>Enabled</Trans>
        </ToggleGroupItem>
        <ToggleGroupItem value={FeatureFlagAudienceState.Disabled} className="min-w-[5rem] justify-center">
          <Trans>Disabled</Trans>
        </ToggleGroupItem>
      </ToggleGroup>

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
    </div>
  );
}
