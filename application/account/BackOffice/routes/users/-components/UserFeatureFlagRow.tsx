import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { Button } from "@repo/ui/components/Button";
import { Switch } from "@repo/ui/components/Switch";
import { TableCell, TableRow } from "@repo/ui/components/Table";
import { Tooltip, TooltipContent, TooltipTrigger } from "@repo/ui/components/Tooltip";
import { getFeatureFlagName, getFeatureFlagSourceLabel } from "@repo/ui/featureFlags/labels";
import { Link } from "@tanstack/react-router";
import { XIcon } from "lucide-react";
import { useEffect, useState } from "react";
import { toast } from "sonner";

import type { components } from "@/shared/lib/api/client";

import { api, queryClient } from "@/shared/lib/api/client";

import { ScopeIcon } from "../../feature-flags/-components/ScopeIcon";

type UserFeatureFlagInfo = components["schemas"]["UserFeatureFlagInfo"];

export function UserFeatureFlagRow({ userId, flag }: Readonly<{ userId: string; flag: UserFeatureFlagInfo }>) {
  const [optimisticEnabled, setOptimisticEnabled] = useState(flag.isEnabled);
  const overrideMutation = api.useMutation("put", "/api/back-office/feature-flags/{flagKey}/user-override");
  const removeMutation = api.useMutation("delete", "/api/back-office/feature-flags/{flagKey}/user-override");

  useEffect(() => {
    setOptimisticEnabled(flag.isEnabled);
  }, [flag.isEnabled]);

  const flagName = getFeatureFlagName(flag.flagKey);

  const handleToggle = (checked: boolean) => {
    setOptimisticEnabled(checked);
    overrideMutation.mutate(
      {
        params: { path: { flagKey: flag.flagKey } },
        body: { userId, tenantId: flag.tenantId, enabled: checked }
      },
      {
        onSuccess: async () => {
          await queryClient.invalidateQueries({
            queryKey: ["get", "/api/back-office/users/{id}/feature-flags"]
          });
          const message = checked ? t`${flagName} enabled` : t`${flagName} disabled`;
          toast.success(message);
        },
        onError: () => {
          setOptimisticEnabled(flag.isEnabled);
        }
      }
    );
  };

  const handleRemoveOverride = () => {
    removeMutation.mutate(
      {
        params: {
          path: { flagKey: flag.flagKey },
          query: { userId, tenantId: flag.tenantId }
        }
      },
      {
        onSuccess: async () => {
          await queryClient.invalidateQueries({
            queryKey: ["get", "/api/back-office/users/{id}/feature-flags"]
          });
          toast.success(t`Override removed for ${flagName}`);
        }
      }
    );
  };

  const isPending = overrideMutation.isPending || removeMutation.isPending;

  return (
    <TableRow rowKey={flag.flagKey}>
      <TableCell>
        <Link
          to="/feature-flags/$flagKey"
          params={{ flagKey: flag.flagKey }}
          className="flex min-w-0 items-center gap-2 outline-none hover:underline focus-visible:underline"
        >
          <ScopeIcon scope={flag.scope} />
          <span className="truncate font-medium">{flagName}</span>
        </Link>
      </TableCell>
      <TableCell className="hidden text-muted-foreground sm:table-cell">{flag.rolloutBucket}</TableCell>
      <TableCell className="hidden md:table-cell">
        <span className="text-sm text-muted-foreground">{getFeatureFlagSourceLabel(flag.source)}</span>
      </TableCell>
      <TableCell className="text-right" onClick={(event) => event.stopPropagation()}>
        <div className="flex items-center justify-end gap-2">
          {flag.source === "manual_override" && (
            <Tooltip>
              <TooltipTrigger
                render={
                  <Button
                    variant="ghost"
                    size="icon"
                    className="size-7"
                    onClick={handleRemoveOverride}
                    disabled={isPending}
                    aria-label={t`Remove override for ${flagName}`}
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
            className={!flag.isBaseRowActive && optimisticEnabled ? "opacity-50" : ""}
            aria-label={t`Override for ${flagName}`}
          />
        </div>
      </TableCell>
    </TableRow>
  );
}
