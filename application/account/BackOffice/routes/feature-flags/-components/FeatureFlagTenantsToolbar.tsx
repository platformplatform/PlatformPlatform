import { t } from "@lingui/core/macro";
import { ToggleGroup, ToggleGroupItem } from "@repo/ui/components/ToggleGroup";
import { useNavigate } from "@tanstack/react-router";

import { SubscriptionPlan } from "@/shared/lib/api/client";
import { getSubscriptionPlanLabel } from "@/shared/lib/api/labels";

import type { StateFilter } from "./stateFilter";

import { FeatureFlagAudienceToolbar } from "./FeatureFlagAudienceToolbar";

interface FeatureFlagTenantsToolbarProps {
  flagKey: string;
  search: string | undefined;
  plans: SubscriptionPlan[];
  state: StateFilter | undefined;
  hasOverride: boolean;
  hideHasOverride?: boolean;
  hideState?: boolean;
}

export function FeatureFlagTenantsToolbar({
  flagKey,
  search,
  plans,
  state,
  hasOverride,
  hideHasOverride,
  hideState
}: Readonly<FeatureFlagTenantsToolbarProps>) {
  const navigate = useNavigate();

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
    <FeatureFlagAudienceToolbar
      search={search}
      searchPlaceholder={t`Search by account name or owner email`}
      state={state}
      hasOverride={hasOverride}
      hideHasOverride={hideHasOverride}
      hideState={hideState}
      onSearchChange={(next) =>
        navigate({
          to: "/feature-flags/$flagKey",
          params: { flagKey },
          search: (previous) => ({ ...previous, tenantsSearch: next, tenantsPageOffset: undefined })
        })
      }
      onStateChange={(next) =>
        navigate({
          to: "/feature-flags/$flagKey",
          params: { flagKey },
          search: (previous) => ({ ...previous, tenantsState: next, tenantsPageOffset: undefined })
        })
      }
      onHasOverrideChange={(next) =>
        navigate({
          to: "/feature-flags/$flagKey",
          params: { flagKey },
          search: (previous) => ({
            ...previous,
            tenantsHasOverride: next ? true : undefined,
            tenantsPageOffset: undefined
          })
        })
      }
    >
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
    </FeatureFlagAudienceToolbar>
  );
}
