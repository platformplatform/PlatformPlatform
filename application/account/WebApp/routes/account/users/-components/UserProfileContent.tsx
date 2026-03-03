import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { Avatar, AvatarFallback, AvatarImage } from "@repo/ui/components/Avatar";
import { Badge } from "@repo/ui/components/Badge";
import { Button } from "@repo/ui/components/Button";
import { Separator } from "@repo/ui/components/Separator";
import { Tooltip, TooltipContent, TooltipTrigger } from "@repo/ui/components/Tooltip";
import { useFormatDate } from "@repo/ui/hooks/useSmartDate";
import { getInitials } from "@repo/utils/string/getInitials";
import { PencilIcon } from "lucide-react";

import type { UserRole } from "@/shared/lib/api/client";

import { getUserRoleLabel } from "@/shared/lib/api/userRole";

interface UserData {
  id: string;
  avatarUrl: string | null;
  firstName: string | null;
  lastName: string | null;
  email: string;
  title: string | null;
  role: string;
  emailConfirmed: boolean;
  createdAt: string;
  lastSeenAt: string | null;
}

export function UserProfileContent({
  user,
  canModifyUser,
  isCurrentUser,
  onChangeRole
}: Readonly<{
  user: UserData;
  canModifyUser: boolean;
  isCurrentUser: boolean;
  onChangeRole: () => void;
}>) {
  const formatDate = useFormatDate();

  return (
    <>
      {/* User Avatar and Basic Info */}
      <div className="mb-6 text-center">
        <Avatar className="mx-auto mb-3 size-16">
          <AvatarImage src={user.avatarUrl ?? undefined} />
          <AvatarFallback>
            {getInitials(user.firstName ?? undefined, user.lastName ?? undefined, user.email)}
          </AvatarFallback>
        </Avatar>
        <h4>
          {user.firstName} {user.lastName}
        </h4>
        {user.title && <span className="block text-sm text-muted-foreground">{user.title}</span>}
      </div>

      {/* Contact Information */}
      <div className="mb-4">
        <div className="space-y-2">
          <div className="flex items-start justify-between">
            <p className="text-sm">
              <Trans>Email</Trans>
            </p>
            <div className="flex flex-col items-end gap-1">
              <p className="text-right text-sm">{user.email}</p>
              {user.emailConfirmed ? (
                <Badge variant="secondary" className="bg-success text-xs text-success-foreground">
                  <Trans>Verified</Trans>
                </Badge>
              ) : (
                <Badge variant="outline" className="text-xs">
                  <Trans>Pending</Trans>
                </Badge>
              )}
            </div>
          </div>
        </div>
      </div>

      <Separator className="mb-4" />

      {/* Role Information */}
      <div className="mb-4 flex items-center justify-between">
        <span className="text-sm font-semibold">
          <Trans>Role</Trans>
        </span>
        {canModifyUser ? (
          <Button
            variant="outline"
            className="h-[var(--control-height-xs)] gap-2 px-3 text-sm"
            onClick={onChangeRole}
            aria-label={t`Change user role for ${`${user.firstName ?? ""} ${user.lastName ?? ""}`.trim() || user.email}`}
          >
            {getUserRoleLabel(user.role as UserRole)}
            <PencilIcon className="size-3 text-muted-foreground" />
          </Button>
        ) : (
          <Tooltip>
            <TooltipTrigger render={<span className="inline-block" />}>
              <Button
                variant="outline"
                className="pointer-events-none h-[var(--control-height-xs)] px-3 text-sm"
                disabled={true}
              >
                {getUserRoleLabel(user.role as UserRole)}
              </Button>
            </TooltipTrigger>
            <TooltipContent>
              {isCurrentUser ? t`You cannot change your own role` : t`Only owners can change user roles`}
            </TooltipContent>
          </Tooltip>
        )}
      </div>

      <Separator className="mb-4" />

      {/* Account Details */}
      <div className="mb-4">
        <div className="space-y-4">
          <div className="flex justify-between">
            <p className="text-sm">
              <Trans>Created</Trans>
            </p>
            <p className="text-sm">{formatDate(user.createdAt, true)}</p>
          </div>
          <div className="flex justify-between">
            <p className="text-sm">
              <Trans>Last seen</Trans>
            </p>
            <p className="text-sm">{formatDate(user.lastSeenAt, true)}</p>
          </div>
        </div>
      </div>
    </>
  );
}
