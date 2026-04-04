import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { Button } from "@repo/ui/components/Button";
import { Switch } from "@repo/ui/components/Switch";
import { TableCell, TableRow } from "@repo/ui/components/Table";
import { Tooltip, TooltipContent, TooltipTrigger } from "@repo/ui/components/Tooltip";
import { XIcon } from "lucide-react";
import { useEffect, useState } from "react";
import { toast } from "sonner";

import { api, queryClient } from "@/shared/lib/api/client";

import type { FlagTenantInfo } from "./types";

function getSourceLabel(source: string): string {
  switch (source) {
    case "manual_override":
      return t`Manual override`;
    case "ab_rollout":
      return t`A/B rollout`;
    case "default":
      return t`Default`;
    default:
      return source;
  }
}

export function TenantOverrideRow({
  flagKey,
  flagDescription,
  tenant,
  showBucket,
  isFlagActive
}: Readonly<{
  flagKey: string;
  flagDescription: string;
  tenant: FlagTenantInfo;
  showBucket: boolean;
  isFlagActive: boolean;
}>) {
  const [optimisticEnabled, setOptimisticEnabled] = useState(tenant.isEnabled);
  const overrideMutation = api.useMutation("put", "/api/back-office/feature-flags/{flagKey}/tenant-override");
  const removeMutation = api.useMutation("delete", "/api/back-office/feature-flags/{flagKey}/tenant-override");

  useEffect(() => {
    if (!overrideMutation.isPending) {
      setOptimisticEnabled(tenant.isEnabled);
    }
  }, [tenant.isEnabled, overrideMutation.isPending]);

  const handleToggle = (checked: boolean) => {
    setOptimisticEnabled(checked);
    overrideMutation.mutate(
      {
        params: { path: { flagKey } },
        body: { tenantId: Number(tenant.tenantId), enabled: checked }
      },
      {
        onSuccess: () => {
          queryClient.invalidateQueries({
            queryKey: ["get", "/api/back-office/feature-flags/{flagKey}/tenants"]
          });
          const message = checked
            ? t`${flagDescription} enabled for ${tenant.tenantName}`
            : t`${flagDescription} disabled for ${tenant.tenantName}`;
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
        params: { path: { flagKey }, query: { tenantId: Number(tenant.tenantId) } }
      },
      {
        onSuccess: () => {
          queryClient.invalidateQueries({
            queryKey: ["get", "/api/back-office/feature-flags/{flagKey}/tenants"]
          });
          toast.success(t`Override removed for ${tenant.tenantName}`);
        }
      }
    );
  };

  const isPending = overrideMutation.isPending || removeMutation.isPending;

  return (
    <TableRow>
      <TableCell className="text-muted-foreground">{tenant.tenantId}</TableCell>
      <TableCell className="font-medium">{tenant.tenantName}</TableCell>
      <TableCell>
        <span className="text-sm text-muted-foreground">{getSourceLabel(tenant.source)}</span>
      </TableCell>
      {showBucket && <TableCell className="text-muted-foreground">{tenant.rolloutBucket}</TableCell>}
      <TableCell className="text-right">
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
                    aria-label={t`Remove override for ${tenant.tenantName}`}
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
            className={!isFlagActive && optimisticEnabled ? "opacity-50" : ""}
            aria-label={t`Override for ${tenant.tenantName}`}
          />
        </div>
      </TableCell>
    </TableRow>
  );
}
