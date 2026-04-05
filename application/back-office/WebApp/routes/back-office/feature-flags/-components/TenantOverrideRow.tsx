import { t } from "@lingui/core/macro";
import { Switch } from "@repo/ui/components/Switch";
import { TableCell, TableRow } from "@repo/ui/components/Table";
import { XIcon } from "lucide-react";
import { useEffect, useState } from "react";
import { toast } from "sonner";

import { api, queryClient } from "@/shared/lib/api/client";

import type { FeatureFlagTenantInfo } from "./types";

function getSourceLabel(source: string): string {
  switch (source) {
    case "ManualOverride":
      return t`Manual override`;
    case "AbRollout":
      return t`A/B rollout`;
    case "Default":
      return t`Default`;
    default:
      return source;
  }
}

export function TenantOverrideRow({
  flagKey,
  featureFlagDescription,
  tenant,
  showRolloutBucket,
  isFeatureFlagActive
}: Readonly<{
  flagKey: string;
  featureFlagDescription: string;
  tenant: FeatureFlagTenantInfo;
  showRolloutBucket: boolean;
  isFeatureFlagActive: boolean;
}>) {
  const [optimisticEnabled, setOptimisticEnabled] = useState(tenant.isEnabled);
  const overrideMutation = api.useMutation("put", "/api/back-office/feature-flags/{featureFlagKey}/tenant-override");
  const removeMutation = api.useMutation("delete", "/api/back-office/feature-flags/{featureFlagKey}/tenant-override");

  useEffect(() => {
    if (!overrideMutation.isPending) {
      setOptimisticEnabled(tenant.isEnabled);
    }
  }, [tenant.isEnabled, overrideMutation.isPending]);

  const handleToggle = (checked: boolean) => {
    setOptimisticEnabled(checked);
    overrideMutation.mutate(
      {
        params: { path: { featureFlagKey: flagKey } },
        body: { tenantId: tenant.tenantId, enabled: checked }
      },
      {
        onSuccess: () => {
          queryClient.invalidateQueries({
            queryKey: ["get", "/api/back-office/feature-flags/{featureFlagKey}/tenants"]
          });
          const message = checked
            ? t`${featureFlagDescription} enabled for ${tenant.tenantName}`
            : t`${featureFlagDescription} disabled for ${tenant.tenantName}`;
          toast.success(message);
        },
        onError: () => {
          setOptimisticEnabled(tenant.isEnabled);
        }
      }
    );
  };

  const handleRemoveOverride = () => {
    removeMutation.mutate(
      {
        params: { path: { featureFlagKey: flagKey }, query: { tenantId: tenant.tenantId } }
      },
      {
        onSuccess: () => {
          queryClient.invalidateQueries({
            queryKey: ["get", "/api/back-office/feature-flags/{featureFlagKey}/tenants"]
          });
          toast.success(t`Override removed for ${tenant.tenantName}`);
        }
      }
    );
  };

  const isPending = overrideMutation.isPending || removeMutation.isPending;

  return (
    <TableRow>
      <TableCell className="hidden truncate text-muted-foreground lg:table-cell">{tenant.tenantId}</TableCell>
      <TableCell className="truncate font-medium">{tenant.tenantName}</TableCell>
      <TableCell className="hidden sm:table-cell">
        {tenant.source === "ManualOverride" ? (
          <button
            type="button"
            className="inline-flex cursor-pointer items-center gap-1 truncate rounded-md border px-2 py-0.5 text-sm text-muted-foreground hover:bg-accent disabled:cursor-not-allowed disabled:opacity-50"
            onClick={handleRemoveOverride}
            disabled={isPending}
            aria-label={t`Remove override for ${tenant.tenantName}`}
          >
            {getSourceLabel(tenant.source)}
            <XIcon className="size-3 shrink-0" />
          </button>
        ) : (
          <span className="text-sm text-muted-foreground">{getSourceLabel(tenant.source)}</span>
        )}
      </TableCell>
      {showRolloutBucket && (
        <TableCell className="hidden text-muted-foreground sm:table-cell">
          {tenant.rolloutBucket === 100 ? t`Always` : tenant.rolloutBucket}
        </TableCell>
      )}
      <TableCell className="text-right">
        <Switch
          checked={optimisticEnabled}
          onCheckedChange={handleToggle}
          disabled={isPending}
          className={!isFeatureFlagActive && optimisticEnabled ? "opacity-50" : ""}
          aria-label={t`Toggle ${featureFlagDescription} for ${tenant.tenantName}`}
        />
      </TableCell>
    </TableRow>
  );
}
