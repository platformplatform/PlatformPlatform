import { t } from "@lingui/core/macro";
import { Switch } from "@repo/ui/components/Switch";
import { TableCell, TableRow } from "@repo/ui/components/Table";
import { useMutation } from "@tanstack/react-query";
import { XIcon } from "lucide-react";
import { useEffect, useState } from "react";
import { toast } from "sonner";

import { apiClient, queryClient } from "@/shared/lib/api/client";

import type { FeatureFlagUserInfo } from "./types";

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

export function UserOverrideRow({
  flagKey,
  featureFlagDescription,
  user,
  showRolloutBucket,
  isFeatureFlagActive
}: Readonly<{
  flagKey: string;
  featureFlagDescription: string;
  user: FeatureFlagUserInfo;
  showRolloutBucket: boolean;
  isFeatureFlagActive: boolean;
}>) {
  const [optimisticEnabled, setOptimisticEnabled] = useState(user.isEnabled);

  const overrideMutation = useMutation({
    mutationFn: async (vars: { enabled: boolean }) => {
      const { error } = await apiClient.PUT("/api/back-office/feature-flags/{featureFlagKey}/user-override", {
        params: { path: { featureFlagKey: flagKey } },
        body: { userId: user.userId, tenantId: user.tenantId, enabled: vars.enabled }
      });
      if (error) throw error;
    }
  });

  const removeMutation = useMutation({
    mutationFn: async () => {
      const { error } = await apiClient.DELETE("/api/back-office/feature-flags/{featureFlagKey}/user-override", {
        params: { path: { featureFlagKey: flagKey }, query: { userId: user.userId, tenantId: user.tenantId } }
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
            queryKey: ["get", "/api/back-office/feature-flags/{featureFlagKey}/users"]
          });
          const message = checked
            ? t`${featureFlagDescription} enabled for ${user.email}`
            : t`${featureFlagDescription} disabled for ${user.email}`;
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
          queryKey: ["get", "/api/back-office/feature-flags/{featureFlagKey}/users"]
        });
        toast.success(t`Override removed for ${user.email}`);
      }
    });
  };

  const isPending = overrideMutation.isPending || removeMutation.isPending;

  return (
    <TableRow>
      <TableCell className="truncate font-medium">{user.email}</TableCell>
      <TableCell className="hidden truncate text-muted-foreground sm:table-cell">{user.tenantName}</TableCell>
      <TableCell className="hidden sm:table-cell">
        {user.source === "ManualOverride" ? (
          <button
            type="button"
            className="inline-flex cursor-pointer items-center gap-1 truncate rounded-md border px-2 py-0.5 text-sm text-muted-foreground hover:bg-accent disabled:cursor-not-allowed disabled:opacity-50"
            onClick={handleRemoveOverride}
            disabled={isPending}
            aria-label={t`Remove override for ${user.email}`}
          >
            {getSourceLabel(user.source)}
            <XIcon className="size-3 shrink-0" />
          </button>
        ) : (
          <span className="text-sm text-muted-foreground">{getSourceLabel(user.source)}</span>
        )}
      </TableCell>
      {showRolloutBucket && (
        <TableCell className="hidden text-muted-foreground sm:table-cell">
          {user.rolloutBucket === 100 ? t`Always` : user.rolloutBucket}
        </TableCell>
      )}
      <TableCell className="text-right">
        <Switch
          checked={optimisticEnabled}
          onCheckedChange={handleToggle}
          disabled={isPending}
          className={!isFeatureFlagActive && optimisticEnabled ? "opacity-50" : ""}
          aria-label={t`Toggle ${featureFlagDescription} for ${user.email}`}
        />
      </TableCell>
    </TableRow>
  );
}
