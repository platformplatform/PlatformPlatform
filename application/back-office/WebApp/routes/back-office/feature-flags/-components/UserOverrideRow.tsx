import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { Button } from "@repo/ui/components/Button";
import { Switch } from "@repo/ui/components/Switch";
import { TableCell, TableRow } from "@repo/ui/components/Table";
import { Tooltip, TooltipContent, TooltipTrigger } from "@repo/ui/components/Tooltip";
import { useMutation } from "@tanstack/react-query";
import { XIcon } from "lucide-react";
import { useEffect, useState } from "react";
import { toast } from "sonner";

import { apiClient, queryClient } from "@/shared/lib/api/client";

import type { FlagUserInfo } from "./types";

function getSourceLabel(source: string): string {
  switch (source) {
    case "manual_override":
      return t`Manual override`;
    case "ab_rollout":
      return t`A/B rollout`;
    case "plan":
      return t`Plan`;
    case "default":
      return t`Default`;
    default:
      return source;
  }
}

export function UserOverrideRow({
  flagKey,
  flagDescription,
  user,
  showBucket,
  isFlagActive
}: Readonly<{
  flagKey: string;
  flagDescription: string;
  user: FlagUserInfo;
  showBucket: boolean;
  isFlagActive: boolean;
}>) {
  const [optimisticEnabled, setOptimisticEnabled] = useState(user.isEnabled);

  const overrideMutation = useMutation({
    mutationFn: async (vars: { enabled: boolean }) => {
      // oxlint-disable-next-line typescript-eslint/no-explicit-any -- endpoint not yet in OpenAPI spec
      const { error } = await apiClient.PUT("/api/back-office/feature-flags/{flagKey}/user-override" as any, {
        params: { path: { flagKey } },
        body: { userId: user.userId, tenantId: user.tenantId, enabled: vars.enabled }
      });
      if (error) throw error;
    }
  });

  const removeMutation = useMutation({
    mutationFn: async () => {
      // oxlint-disable-next-line typescript-eslint/no-explicit-any -- endpoint not yet in OpenAPI spec
      const { error } = await apiClient.DELETE("/api/back-office/feature-flags/{flagKey}/user-override" as any, {
        params: { path: { flagKey }, query: { userId: user.userId, tenantId: user.tenantId } }
      });
      if (error) throw error;
    }
  });

  useEffect(() => {
    if (!overrideMutation.isPending) {
      setOptimisticEnabled(user.isEnabled);
    }
  }, [user.isEnabled, overrideMutation.isPending]);

  const handleToggle = (checked: boolean) => {
    setOptimisticEnabled(checked);
    overrideMutation.mutate(
      { enabled: checked },
      {
        onSuccess: () => {
          queryClient.invalidateQueries({
            queryKey: ["get", "/api/back-office/feature-flags/{flagKey}/users"]
          });
          const message = checked
            ? t`${flagDescription} enabled for ${user.email}`
            : t`${flagDescription} disabled for ${user.email}`;
          toast.success(message);
        },
        onError: () => {
          setOptimisticEnabled(user.isEnabled);
        }
      }
    );
  };

  const handleRemoveOverride = () => {
    removeMutation.mutate(undefined, {
      onSuccess: () => {
        queryClient.invalidateQueries({
          queryKey: ["get", "/api/back-office/feature-flags/{flagKey}/users"]
        });
        toast.success(t`Override removed for ${user.email}`);
      }
    });
  };

  const isPending = overrideMutation.isPending || removeMutation.isPending;

  return (
    <TableRow>
      <TableCell className="truncate font-medium">{user.email}</TableCell>
      <TableCell className="truncate text-muted-foreground">{user.tenantName}</TableCell>
      <TableCell className="hidden sm:table-cell">
        <span className="text-sm text-muted-foreground">{getSourceLabel(user.source)}</span>
      </TableCell>
      {showBucket && <TableCell className="hidden text-muted-foreground sm:table-cell">{user.rolloutBucket}</TableCell>}
      <TableCell className="text-right">
        <div className="flex items-center justify-end gap-2">
          {user.source === "manual_override" && (
            <Tooltip>
              <TooltipTrigger
                render={
                  <Button
                    variant="ghost"
                    size="icon"
                    className="size-7"
                    onClick={handleRemoveOverride}
                    disabled={isPending}
                    aria-label={t`Remove override for ${user.email}`}
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
            aria-label={t`Override for ${user.email}`}
          />
        </div>
      </TableCell>
    </TableRow>
  );
}
