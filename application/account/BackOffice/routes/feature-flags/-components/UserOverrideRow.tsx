import { t } from "@lingui/core/macro";
import { Avatar, AvatarFallback, AvatarImage } from "@repo/ui/components/Avatar";
import { Badge } from "@repo/ui/components/Badge";
import { TableCell, TableRow } from "@repo/ui/components/Table";
import { useFormatDate } from "@repo/ui/hooks/useSmartDate";
import { useNavigate } from "@tanstack/react-router";
import { MailIcon } from "lucide-react";
import { useEffect, useState } from "react";
import { toast } from "sonner";

import { api, queryClient } from "@/shared/lib/api/client";
import { getUserRoleLabel } from "@/shared/lib/api/labels";

import type { FeatureFlagUserInfo } from "./types";

import { getUserDisplayName, getUserInitials } from "../../users/-components/userDisplay";
import { OverrideSwitch } from "./OverrideSwitch";

// Mutations skip the global auto-invalidate and refetch is deferred 500ms so the user sees the
// optimistic switch flip before the table refetches — without it, sort reordering, pagination
// totals, or filter drops happen instantly and make the toggle feel unresponsive.
const USERS_QUERY_KEY = ["get", "/api/back-office/feature-flags/{flagKey}/users"] as const;
const SKIP_AUTO_INVALIDATE = { meta: { skipQueryInvalidation: true } };

interface UserOverrideRowProps {
  flagKey: string;
  featureFlagDescription: string;
  user: FeatureFlagUserInfo;
  showRolloutBucket: boolean;
  isFeatureFlagActive: boolean;
}

export function UserOverrideRow({
  flagKey,
  featureFlagDescription,
  user,
  showRolloutBucket,
  isFeatureFlagActive
}: Readonly<UserOverrideRowProps>) {
  const [optimisticEnabled, setOptimisticEnabled] = useState(user.isEnabled);
  const [optimisticIsOverride, setOptimisticIsOverride] = useState(user.source === "manual_override");
  const overrideMutation = api.useMutation(
    "put",
    "/api/back-office/feature-flags/{flagKey}/user-override",
    SKIP_AUTO_INVALIDATE
  );
  const removeMutation = api.useMutation(
    "delete",
    "/api/back-office/feature-flags/{flagKey}/user-override",
    SKIP_AUTO_INVALIDATE
  );
  const formatDate = useFormatDate();
  const navigate = useNavigate();

  useEffect(() => {
    setOptimisticEnabled(user.isEnabled);
    setOptimisticIsOverride(user.source === "manual_override");
  }, [user.isEnabled, user.source]);

  const refreshAfter = () => {
    setTimeout(() => queryClient.invalidateQueries({ queryKey: USERS_QUERY_KEY }), 500);
  };

  const handleRemoveOverride = () => {
    setOptimisticEnabled(user.defaultEnabled);
    setOptimisticIsOverride(false);
    removeMutation.mutate(
      { params: { path: { flagKey }, query: { userId: user.id, tenantId: user.tenantId } } },
      {
        onSuccess: () => {
          toast.success(t`Override removed for ${user.email}`, {
            description: t`It takes up to 5 minutes for changes to reach all users.`
          });
          refreshAfter();
        },
        onError: () => {
          setOptimisticEnabled(user.isEnabled);
          setOptimisticIsOverride(user.source === "manual_override");
        }
      }
    );
  };

  const handleSetOverride = (checked: boolean) => {
    setOptimisticEnabled(checked);
    setOptimisticIsOverride(true);
    overrideMutation.mutate(
      {
        params: { path: { flagKey } },
        body: { userId: user.id, tenantId: user.tenantId, enabled: checked }
      },
      {
        onSuccess: () => {
          toast.success(
            checked
              ? t`${featureFlagDescription} enabled for ${user.email}`
              : t`${featureFlagDescription} disabled for ${user.email}`,
            { description: t`It takes up to 5 minutes for changes to reach all users.` }
          );
          refreshAfter();
        },
        onError: () => {
          setOptimisticEnabled(user.isEnabled);
          setOptimisticIsOverride(user.source === "manual_override");
        }
      }
    );
  };

  const handleToggle = (checked: boolean) => {
    if (showRolloutBucket && optimisticIsOverride && optimisticEnabled === user.defaultEnabled) {
      handleRemoveOverride();
      return;
    }
    handleSetOverride(checked);
  };

  const isPending = overrideMutation.isPending || removeMutation.isPending;
  const displayName = getUserDisplayName(user.firstName, user.lastName, user.email);
  const initials = getUserInitials(user.firstName, user.lastName, user.email);

  return (
    <TableRow
      rowKey={user.id}
      className="cursor-pointer transition-opacity duration-500"
      onClick={() => navigate({ to: "/users/$userId", params: { userId: user.id }, search: { tab: "feature-flags" } })}
    >
      <TableCell>
        <div className="flex min-w-0 items-center gap-3">
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
        </div>
      </TableCell>
      <TableCell className="hidden md:table-cell">
        <span className="block truncate text-sm">{user.tenantName}</span>
      </TableCell>
      <TableCell className="hidden lg:table-cell">
        <Badge variant="outline">{getUserRoleLabel(user.role)}</Badge>
      </TableCell>
      <TableCell className="hidden lg:table-cell">
        {(() => {
          const updatedAt = user.overrideDisabledAt ?? user.overrideEnabledAt;
          return updatedAt ? formatDate(updatedAt, true, true) : <span className="text-muted-foreground">-</span>;
        })()}
      </TableCell>
      {showRolloutBucket && (
        <TableCell className="hidden text-center text-muted-foreground lg:table-cell">
          {user.inclusionThresholdPercentage != null ? `${user.inclusionThresholdPercentage}%` : null}
        </TableCell>
      )}
      <TableCell className="text-right" onClick={(event) => event.stopPropagation()}>
        <OverrideSwitch
          isManualOverride={optimisticIsOverride && showRolloutBucket}
          checked={optimisticEnabled}
          onCheckedChange={handleToggle}
          disabled={isPending}
          dimmed={!isFeatureFlagActive && optimisticEnabled}
          ariaLabel={t`Override for ${user.email}`}
        />
      </TableCell>
    </TableRow>
  );
}
