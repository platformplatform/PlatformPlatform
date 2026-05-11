import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { Avatar, AvatarFallback, AvatarImage } from "@repo/ui/components/Avatar";
import { Badge } from "@repo/ui/components/Badge";
import { Button } from "@repo/ui/components/Button";
import { Switch } from "@repo/ui/components/Switch";
import { TableCell, TableRow } from "@repo/ui/components/Table";
import { Tooltip, TooltipContent, TooltipTrigger } from "@repo/ui/components/Tooltip";
import { useFormatDate } from "@repo/ui/hooks/useSmartDate";
import { Link } from "@tanstack/react-router";
import { MailIcon, XIcon } from "lucide-react";
import { useEffect, useState } from "react";
import { toast } from "sonner";

import { api, queryClient } from "@/shared/lib/api/client";
import { getUserRoleLabel } from "@/shared/lib/api/labels";

import type { FeatureFlagUserInfo } from "./types";

import { getUserDisplayName, getUserInitials } from "../../users/-components/userDisplay";
import { getFeatureFlagSourceLabel } from "./flagLabels";

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
  const overrideMutation = api.useMutation("put", "/api/back-office/feature-flags/{flagKey}/user-override");
  const removeMutation = api.useMutation("delete", "/api/back-office/feature-flags/{flagKey}/user-override");
  const formatDate = useFormatDate();

  useEffect(() => {
    setOptimisticEnabled(user.isEnabled);
  }, [user.isEnabled]);

  const handleToggle = (checked: boolean) => {
    setOptimisticEnabled(checked);
    overrideMutation.mutate(
      {
        params: { path: { flagKey } },
        body: { userId: user.id, tenantId: user.tenantId, enabled: checked }
      },
      {
        onSuccess: async () => {
          await queryClient.invalidateQueries({
            queryKey: ["get", "/api/back-office/feature-flags/{flagKey}/users"]
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
    removeMutation.mutate(
      {
        params: { path: { flagKey }, query: { userId: user.id, tenantId: user.tenantId } }
      },
      {
        onSuccess: async () => {
          await queryClient.invalidateQueries({
            queryKey: ["get", "/api/back-office/feature-flags/{flagKey}/users"]
          });
          toast.success(t`Override removed for ${user.email}`);
        }
      }
    );
  };

  const isPending = overrideMutation.isPending || removeMutation.isPending;
  const displayName = getUserDisplayName(user.firstName, user.lastName, user.email);
  const initials = getUserInitials(user.firstName, user.lastName, user.email);

  return (
    <TableRow rowKey={user.id}>
      <TableCell>
        <Link
          to="/users/$userId"
          params={{ userId: user.id }}
          className="flex min-w-0 items-center gap-3 outline-none hover:underline focus-visible:underline"
          aria-label={t`Open user ${displayName}`}
        >
          <Avatar size="default" className="size-9 shrink-0">
            {user.avatarUrl && <AvatarImage src={user.avatarUrl} alt={displayName} />}
            <AvatarFallback>{initials}</AvatarFallback>
          </Avatar>
          <div className="flex min-w-0 flex-col gap-0.5">
            <span className="truncate font-medium text-foreground">{displayName}</span>
            <span className="flex min-w-0 items-center gap-1.5 text-xs text-muted-foreground">
              <MailIcon className="size-3 shrink-0" aria-hidden={true} />
              <span className="truncate">{user.email}</span>
            </span>
          </div>
        </Link>
      </TableCell>
      <TableCell className="hidden md:table-cell">
        <span className="truncate text-sm">{user.tenantName}</span>
      </TableCell>
      <TableCell className="hidden lg:table-cell">
        <Badge variant="outline">{getUserRoleLabel(user.role)}</Badge>
      </TableCell>
      <TableCell className="hidden lg:table-cell">
        {user.lastSeenAt ? formatDate(user.lastSeenAt, true, true) : <span className="text-muted-foreground">-</span>}
      </TableCell>
      <TableCell className="hidden sm:table-cell">
        <span className="text-sm text-muted-foreground">{getFeatureFlagSourceLabel(user.source)}</span>
      </TableCell>
      {showRolloutBucket && (
        <TableCell className="hidden text-muted-foreground sm:table-cell">{user.rolloutBucket}</TableCell>
      )}
      <TableCell className="text-right" onClick={(event) => event.stopPropagation()}>
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
            className={!isFeatureFlagActive && optimisticEnabled ? "opacity-50" : ""}
            aria-label={t`Override for ${user.email}`}
          />
        </div>
      </TableCell>
    </TableRow>
  );
}
