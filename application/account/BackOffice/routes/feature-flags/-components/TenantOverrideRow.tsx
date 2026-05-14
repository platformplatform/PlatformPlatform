import { t } from "@lingui/core/macro";
import { Badge } from "@repo/ui/components/Badge";
import { TableCell, TableRow } from "@repo/ui/components/Table";
import { TenantLogo } from "@repo/ui/components/TenantLogo";
import { useFormatDate } from "@repo/ui/hooks/useSmartDate";
import { useNavigate } from "@tanstack/react-router";
import { useEffect, useState } from "react";
import { toast } from "sonner";

import { api, FeatureFlagAudienceState, queryClient } from "@/shared/lib/api/client";
import { getSubscriptionPlanLabel } from "@/shared/lib/api/labels";
import { getSubscriptionPlanBadgeClass } from "@/shared/lib/planBadge";

import type { FeatureFlagTenantInfo } from "./types";

import { MrrCell } from "../../accounts/-components/MrrCell";
import { TenantStatusBadge } from "../../accounts/-components/TenantStatusBadge";
import { getUserDisplayName } from "../../users/-components/userDisplay";
import { OverrideSwitch } from "./OverrideSwitch";
import { type StateFilter, toApiState } from "./stateFilter";

// Mutations skip the global auto-invalidate so we can manually delay invalidation when the click
// will cause the row to leave the active filter — without the 500ms grace the row vanishes the
// instant the user clicks, hiding the cause/effect.
const TENANTS_QUERY_KEY = ["get", "/api/back-office/feature-flags/{flagKey}/tenants"] as const;
const SKIP_AUTO_INVALIDATE = { meta: { skipQueryInvalidation: true } };

interface TenantOverrideRowProps {
  flagKey: string;
  featureFlagDescription: string;
  tenant: FeatureFlagTenantInfo;
  showRolloutBucket: boolean;
  isFeatureFlagActive: boolean;
  stateFilter: StateFilter | undefined;
  hasOverrideFilter: boolean;
}

export function TenantOverrideRow({
  flagKey,
  featureFlagDescription,
  tenant,
  showRolloutBucket,
  isFeatureFlagActive,
  stateFilter,
  hasOverrideFilter
}: Readonly<TenantOverrideRowProps>) {
  const [optimisticEnabled, setOptimisticEnabled] = useState(tenant.isEnabled);
  const [optimisticIsOverride, setOptimisticIsOverride] = useState(tenant.source === "manual_override");
  const overrideMutation = api.useMutation(
    "put",
    "/api/back-office/feature-flags/{flagKey}/tenant-override",
    SKIP_AUTO_INVALIDATE
  );
  const removeMutation = api.useMutation(
    "delete",
    "/api/back-office/feature-flags/{flagKey}/tenant-override",
    SKIP_AUTO_INVALIDATE
  );
  const formatDate = useFormatDate();
  const navigate = useNavigate();

  useEffect(() => {
    setOptimisticEnabled(tenant.isEnabled);
    setOptimisticIsOverride(tenant.source === "manual_override");
  }, [tenant.isEnabled, tenant.source]);

  const refreshAfter = (finalEnabled: boolean, willHaveOverride: boolean) => {
    const apiState = toApiState(stateFilter);
    const stateFilterWillDrop =
      (apiState === FeatureFlagAudienceState.Enabled && !finalEnabled) ||
      (apiState === FeatureFlagAudienceState.Disabled && finalEnabled);
    const hasOverrideFilterWillDrop = hasOverrideFilter && !willHaveOverride;
    const delayMs = stateFilterWillDrop || hasOverrideFilterWillDrop ? 500 : 0;
    setTimeout(() => queryClient.invalidateQueries({ queryKey: TENANTS_QUERY_KEY }), delayMs);
  };

  const handleRemoveOverride = () => {
    setOptimisticEnabled(tenant.defaultEnabled);
    setOptimisticIsOverride(false);
    removeMutation.mutate(
      { params: { path: { flagKey }, query: { tenantId: tenant.id } } },
      {
        onSuccess: () => {
          toast.success(t`Override removed for ${tenant.name}`);
          refreshAfter(tenant.defaultEnabled, false);
        },
        onError: () => {
          setOptimisticEnabled(tenant.isEnabled);
          setOptimisticIsOverride(tenant.source === "manual_override");
        }
      }
    );
  };

  const handleSetOverride = (checked: boolean) => {
    setOptimisticEnabled(checked);
    setOptimisticIsOverride(true);
    overrideMutation.mutate(
      { params: { path: { flagKey } }, body: { tenantId: tenant.id, enabled: checked } },
      {
        onSuccess: () => {
          toast.success(
            checked
              ? t`${featureFlagDescription} enabled for ${tenant.name}`
              : t`${featureFlagDescription} disabled for ${tenant.name}`
          );
          refreshAfter(checked, true);
        },
        onError: () => {
          setOptimisticEnabled(tenant.isEnabled);
          setOptimisticIsOverride(tenant.source === "manual_override");
        }
      }
    );
  };

  const handleToggle = (checked: boolean) => {
    // The "single-click clears redundant override" rule only applies to A/B-eligible flags. For non-A/B
    // flags the override is the only mechanism to turn the flag on for an account, so every click must
    // PUT (or update) the override; we never auto-remove based on state matching defaultEnabled (which
    // is always false on non-A/B flags and would otherwise trigger spurious removals on every toggle-off).
    if (showRolloutBucket && optimisticIsOverride && optimisticEnabled === tenant.defaultEnabled) {
      handleRemoveOverride();
      return;
    }
    handleSetOverride(checked);
  };

  const isPending = overrideMutation.isPending || removeMutation.isPending;
  const ownerLabel = tenant.owner
    ? getUserDisplayName(tenant.owner.firstName, tenant.owner.lastName, tenant.owner.email)
    : null;

  return (
    <TableRow
      rowKey={tenant.id}
      className="cursor-pointer transition-opacity duration-500"
      onClick={() =>
        navigate({ to: "/accounts/$tenantId", params: { tenantId: tenant.id }, search: { tab: "feature-flags" } })
      }
    >
      <TableCell>
        <div className="flex min-w-0 items-center gap-3">
          <TenantLogo logoUrl={tenant.logoUrl} tenantName={tenant.name} size="md" className="size-9 shrink-0" />
          <div className="flex min-w-0 flex-col gap-0.5">
            <span className="truncate font-medium text-foreground">{tenant.name}</span>
            {ownerLabel && <span className="truncate text-xs text-muted-foreground">{ownerLabel}</span>}
          </div>
        </div>
      </TableCell>
      <TableCell className="hidden md:table-cell">
        <Badge className={getSubscriptionPlanBadgeClass(tenant.plan)}>{getSubscriptionPlanLabel(tenant.plan)}</Badge>
      </TableCell>
      <TableCell className="hidden tabular-nums lg:table-cell">
        <MrrCell
          monthlyRecurringRevenue={tenant.monthlyRecurringRevenue}
          scheduledPriceAmount={tenant.scheduledPriceAmount}
          currency={tenant.currency}
          plannedChange={tenant.plannedChange}
        />
      </TableCell>
      <TableCell className="hidden lg:table-cell">
        {tenant.renewalDate ? formatDate(tenant.renewalDate) : <span className="text-muted-foreground">-</span>}
      </TableCell>
      <TableCell className="hidden md:table-cell">
        <TenantStatusBadge
          plan={tenant.plan}
          plannedChange={tenant.plannedChange}
          hasEverSubscribed={tenant.hasEverSubscribed}
        />
      </TableCell>
      {showRolloutBucket && (
        <TableCell className="hidden text-center text-muted-foreground lg:table-cell">
          {tenant.inclusionThresholdPercentage != null ? `${tenant.inclusionThresholdPercentage}%` : null}
        </TableCell>
      )}
      <TableCell className="text-right" onClick={(event) => event.stopPropagation()}>
        <OverrideSwitch
          isManualOverride={optimisticIsOverride && showRolloutBucket}
          checked={optimisticEnabled}
          onCheckedChange={handleToggle}
          disabled={isPending}
          dimmed={!isFeatureFlagActive && optimisticEnabled}
          ariaLabel={t`Override for ${tenant.name}`}
        />
      </TableCell>
    </TableRow>
  );
}
