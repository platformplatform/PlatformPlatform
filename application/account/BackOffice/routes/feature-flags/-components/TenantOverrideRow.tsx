import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { Badge } from "@repo/ui/components/Badge";
import { Button } from "@repo/ui/components/Button";
import { Switch } from "@repo/ui/components/Switch";
import { TableCell, TableRow } from "@repo/ui/components/Table";
import { TenantLogo } from "@repo/ui/components/TenantLogo";
import { Tooltip, TooltipContent, TooltipTrigger } from "@repo/ui/components/Tooltip";
import { getFeatureFlagSourceLabel } from "@repo/ui/featureFlags/labels";
import { useFormatDate } from "@repo/ui/hooks/useSmartDate";
import { Link } from "@tanstack/react-router";
import { XIcon } from "lucide-react";
import { useEffect, useState } from "react";
import { toast } from "sonner";

import { api, queryClient } from "@/shared/lib/api/client";
import { getSubscriptionPlanLabel } from "@/shared/lib/api/labels";
import { getSubscriptionPlanBadgeClass } from "@/shared/lib/planBadge";

import type { FeatureFlagTenantInfo } from "./types";

import { MrrCell } from "../../accounts/-components/MrrCell";
import { TenantStatusBadge } from "../../accounts/-components/TenantStatusBadge";
import { getUserDisplayName } from "../../users/-components/userDisplay";

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
  const overrideMutation = api.useMutation("put", "/api/back-office/feature-flags/{flagKey}/tenant-override");
  const removeMutation = api.useMutation("delete", "/api/back-office/feature-flags/{flagKey}/tenant-override");
  const formatDate = useFormatDate();

  useEffect(() => {
    setOptimisticEnabled(tenant.isEnabled);
  }, [tenant.isEnabled]);

  const handleToggle = (checked: boolean) => {
    setOptimisticEnabled(checked);
    overrideMutation.mutate(
      {
        params: { path: { flagKey } },
        body: { tenantId: tenant.id, enabled: checked }
      },
      {
        onSuccess: async () => {
          await queryClient.invalidateQueries({
            queryKey: ["get", "/api/back-office/feature-flags/{flagKey}/tenants"]
          });
          const message = checked
            ? t`${featureFlagDescription} enabled for ${tenant.name}`
            : t`${featureFlagDescription} disabled for ${tenant.name}`;
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
        params: { path: { flagKey }, query: { tenantId: tenant.id } }
      },
      {
        onSuccess: async () => {
          await queryClient.invalidateQueries({
            queryKey: ["get", "/api/back-office/feature-flags/{flagKey}/tenants"]
          });
          toast.success(t`Override removed for ${tenant.name}`);
        }
      }
    );
  };

  const isPending = overrideMutation.isPending || removeMutation.isPending;
  const ownerLabel = tenant.owner
    ? getUserDisplayName(tenant.owner.firstName, tenant.owner.lastName, tenant.owner.email)
    : null;

  return (
    <TableRow rowKey={tenant.id}>
      <TableCell>
        <Link
          to="/accounts/$tenantId"
          params={{ tenantId: tenant.id }}
          className="flex min-w-0 items-center gap-3 outline-none hover:underline focus-visible:underline"
          aria-label={t`Open account ${tenant.name}`}
        >
          <TenantLogo logoUrl={tenant.logoUrl} tenantName={tenant.name} size="md" className="size-9 shrink-0" />
          <div className="flex min-w-0 flex-col gap-0.5">
            <span className="truncate font-medium text-foreground">{tenant.name}</span>
            {ownerLabel && <span className="truncate text-xs text-muted-foreground">{ownerLabel}</span>}
          </div>
        </Link>
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
      <TableCell className="hidden sm:table-cell">
        <span className="text-sm text-muted-foreground">{getFeatureFlagSourceLabel(tenant.source)}</span>
      </TableCell>
      {showRolloutBucket && (
        <TableCell className="hidden text-muted-foreground sm:table-cell">{tenant.rolloutBucket}</TableCell>
      )}
      <TableCell className="text-right" onClick={(event) => event.stopPropagation()}>
        <div className="flex items-center justify-end gap-2">
          {tenant.source === "manual_override" && (
            <Tooltip>
              <TooltipTrigger
                render={
                  <Button
                    variant="ghost"
                    size="icon"
                    className="size-7"
                    onClick={handleRemoveOverride}
                    disabled={isPending}
                    aria-label={t`Remove override for ${tenant.name}`}
                  />
                }
              >
                <XIcon className="size-4" />
              </TooltipTrigger>
              <TooltipContent>
                <Trans>Remove override</Trans>
              </TooltipContent>
            </Tooltip>
          )}
          <Switch
            checked={optimisticEnabled}
            onCheckedChange={handleToggle}
            disabled={isPending}
            className={!isFeatureFlagActive && optimisticEnabled ? "opacity-50" : ""}
            aria-label={t`Override for ${tenant.name}`}
          />
        </div>
      </TableCell>
    </TableRow>
  );
}
