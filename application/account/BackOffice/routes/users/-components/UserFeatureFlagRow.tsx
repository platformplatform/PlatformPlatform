import { t } from "@lingui/core/macro";
import { TableCell, TableRow } from "@repo/ui/components/Table";
import { getFeatureFlagDescription, getFeatureFlagName } from "@repo/ui/featureFlags/labels";
import { useNavigate } from "@tanstack/react-router";
import { useEffect, useState } from "react";
import { toast } from "sonner";

import type { components } from "@/shared/lib/api/client";

import { api } from "@/shared/lib/api/client";

import { OverrideSwitch } from "../../feature-flags/-components/OverrideSwitch";
import { ScopeIcon } from "../../feature-flags/-components/ScopeIcon";

type UserFeatureFlagInfo = components["schemas"]["UserFeatureFlagInfo"];

export function UserFeatureFlagRow({
  userId,
  flag,
  showBucketColumn
}: Readonly<{ userId: string; flag: UserFeatureFlagInfo; showBucketColumn: boolean }>) {
  const [optimisticEnabled, setOptimisticEnabled] = useState(flag.isEnabled);
  const [optimisticIsOverride, setOptimisticIsOverride] = useState(flag.source === "manual_override");
  // No hover-defer here — the user-detail feature-flag tab has no filters, so the row can never be
  // removed from view by a mutation. The global mutation handler invalidates queries automatically.
  const overrideMutation = api.useMutation("put", "/api/back-office/feature-flags/{flagKey}/user-override");
  const removeMutation = api.useMutation("delete", "/api/back-office/feature-flags/{flagKey}/user-override");
  const navigate = useNavigate();

  useEffect(() => {
    setOptimisticEnabled(flag.isEnabled);
    setOptimisticIsOverride(flag.source === "manual_override");
  }, [flag.isEnabled, flag.source]);

  const flagName = getFeatureFlagName(flag.flagKey);
  const flagDescription = getFeatureFlagDescription(flag.flagKey) || flag.description;

  const handleRemoveOverride = () => {
    setOptimisticEnabled(flag.defaultEnabled);
    setOptimisticIsOverride(false);
    removeMutation.mutate(
      { params: { path: { flagKey: flag.flagKey }, query: { userId, tenantId: flag.tenantId } } },
      {
        onSuccess: () =>
          toast.success(t`Override removed for ${flagName}`, {
            description: t`It takes up to 5 minutes for changes to reach all users.`
          }),
        onError: () => {
          setOptimisticEnabled(flag.isEnabled);
          setOptimisticIsOverride(flag.source === "manual_override");
        }
      }
    );
  };

  const handleSetOverride = (checked: boolean) => {
    setOptimisticEnabled(checked);
    setOptimisticIsOverride(true);
    overrideMutation.mutate(
      { params: { path: { flagKey: flag.flagKey } }, body: { userId, tenantId: flag.tenantId, enabled: checked } },
      {
        onSuccess: () =>
          toast.success(checked ? t`${flagName} enabled` : t`${flagName} disabled`, {
            description: t`It takes up to 5 minutes for changes to reach all users.`
          }),
        onError: () => {
          setOptimisticEnabled(flag.isEnabled);
          setOptimisticIsOverride(flag.source === "manual_override");
        }
      }
    );
  };

  const handleToggle = (checked: boolean) => {
    if (flag.isAbTestEligible && optimisticIsOverride && optimisticEnabled === flag.defaultEnabled) {
      handleRemoveOverride();
      return;
    }
    handleSetOverride(checked);
  };

  const isPending = overrideMutation.isPending || removeMutation.isPending;

  return (
    <TableRow
      rowKey={flag.flagKey}
      className="cursor-pointer"
      onClick={() => navigate({ to: "/feature-flags/$flagKey", params: { flagKey: flag.flagKey } })}
    >
      <TableCell>
        <div className="flex min-w-0 flex-col">
          <span className="flex items-center gap-2 font-medium">
            <ScopeIcon scope={flag.scope} isAbTestEligible={flag.isAbTestEligible} />
            <span className="truncate">{flagName}</span>
          </span>
          {flagDescription && (
            <span className="hidden truncate text-sm text-muted-foreground sm:block">{flagDescription}</span>
          )}
        </div>
      </TableCell>
      {showBucketColumn && (
        <TableCell className="hidden text-center text-muted-foreground sm:table-cell">
          {flag.inclusionThresholdPercentage != null ? `${flag.inclusionThresholdPercentage}%` : null}
        </TableCell>
      )}
      <TableCell className="text-center" onClick={(event) => event.stopPropagation()}>
        <OverrideSwitch
          isManualOverride={optimisticIsOverride && flag.isAbTestEligible}
          checked={optimisticEnabled}
          onCheckedChange={handleToggle}
          disabled={isPending}
          dimmed={!flag.isBaseRowActive && optimisticEnabled}
          ariaLabel={t`Override for ${flagName}`}
        />
      </TableCell>
    </TableRow>
  );
}
