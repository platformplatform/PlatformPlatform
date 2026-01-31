import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { useUserInfo } from "@repo/infrastructure/auth/hooks";
import { Avatar, AvatarFallback, AvatarImage } from "@repo/ui/components/Avatar";
import { Badge } from "@repo/ui/components/Badge";
import { Button } from "@repo/ui/components/Button";
import { Separator } from "@repo/ui/components/Separator";
import { SidePane, SidePaneBody, SidePaneFooter, SidePaneHeader } from "@repo/ui/components/SidePane";
import { Skeleton } from "@repo/ui/components/Skeleton";
import { formatDate } from "@repo/utils/date/formatDate";
import { getInitials } from "@repo/utils/string/getInitials";
import { InfoIcon, Trash2Icon } from "lucide-react";
import { useState } from "react";
import type { components } from "@/shared/lib/api/client";
import { getUserRoleLabel } from "@/shared/lib/api/userRole";
import { ChangeUserRoleDialog } from "./ChangeUserRoleDialog";

type UserDetails = components["schemas"]["UserDetails"];

interface UserProfileSidePaneProps {
  user: UserDetails | null;
  isOpen: boolean;
  onClose: () => void;
  onDeleteUser: (user: UserDetails) => void;
  isUserInCurrentView?: boolean;
  isDataNewer?: boolean;
  isLoading?: boolean;
}

function UserProfileContent({
  user,
  canModifyUser,
  onChangeRole
}: Readonly<{
  user: UserDetails;
  canModifyUser: boolean;
  onChangeRole: () => void;
}>) {
  return (
    <>
      {/* User Avatar and Basic Info */}
      <div className="mb-6 text-center">
        <Avatar className="mx-auto mb-3 size-16">
          <AvatarImage src={user.avatarUrl ?? undefined} />
          <AvatarFallback>{getInitials(user.firstName, user.lastName, user.email)}</AvatarFallback>
        </Avatar>
        <h4>
          {user.firstName} {user.lastName}
        </h4>
        {user.title && <span className="block text-muted-foreground text-sm">{user.title}</span>}
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
                <Badge variant="secondary" className="bg-success text-success-foreground text-xs">
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
        <span className="font-semibold text-sm">
          <Trans>Role</Trans>
        </span>
        {canModifyUser ? (
          <Button
            variant="ghost"
            className="h-auto p-0 text-xs"
            onClick={onChangeRole}
            aria-label={t`Change user role for ${`${user.firstName ?? ""} ${user.lastName ?? ""}`.trim() || user.email}`}
          >
            <Badge
              variant="outline"
              className="cursor-pointer text-xs transition-all duration-200 hover:scale-105 hover:bg-muted hover:shadow-sm"
            >
              {getUserRoleLabel(user.role)}
            </Badge>
          </Button>
        ) : (
          <Badge variant="outline" className="text-xs">
            {getUserRoleLabel(user.role)}
          </Badge>
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

function NoticeBar({ children }: Readonly<{ children: React.ReactNode }>) {
  return (
    <div className="border-border border-b bg-muted px-4 py-3">
      <div className="flex items-center gap-2 text-muted-foreground">
        <InfoIcon className="size-4 flex-shrink-0" />
        <p className="font-medium text-sm">{children}</p>
      </div>
    </div>
  );
}

export function UserProfileSidePane({
  user,
  isOpen,
  onClose,
  onDeleteUser,
  isUserInCurrentView = true,
  isDataNewer = false,
  isLoading = false
}: Readonly<UserProfileSidePaneProps>) {
  const userInfo = useUserInfo();
  const [isChangeRoleDialogOpen, setIsChangeRoleDialogOpen] = useState(false);

  const isCurrentUser = user?.id === userInfo?.id;
  const canModifyUser = userInfo?.role === "Owner" && !isCurrentUser;

  return (
    <>
      <SidePane isOpen={isOpen} onOpenChange={(open) => !open && onClose()} aria-label={t`User profile`}>
        <SidePaneHeader closeButtonLabel={t`Close user profile`}>
          <Trans>User profile</Trans>
        </SidePaneHeader>

        {!isUserInCurrentView && (
          <NoticeBar>
            <Trans>User not in current view</Trans>
          </NoticeBar>
        )}

        {isDataNewer && (
          <NoticeBar>
            <Trans>User data updated</Trans>
          </NoticeBar>
        )}

        <SidePaneBody>
          {isLoading ? (
            <>
              <div className="mb-6 text-center">
                <Skeleton className="mx-auto mb-3 size-20 rounded-full" />
                <Skeleton className="mx-auto mb-2 h-6 w-32" />
                <Skeleton className="mx-auto h-4 w-24" />
              </div>
              <Skeleton className="h-64 w-full" />
            </>
          ) : (
            user && (
              <UserProfileContent
                user={user}
                canModifyUser={canModifyUser}
                onChangeRole={() => setIsChangeRoleDialogOpen(true)}
              />
            )
          )}
        </SidePaneBody>

        {userInfo?.role === "Owner" && user && (
          <SidePaneFooter>
            <Button
              variant="destructive"
              onClick={() => onDeleteUser(user)}
              className="w-full justify-center text-sm"
              disabled={isCurrentUser}
            >
              <Trash2Icon className="size-4" />
              <Trans>Delete user</Trans>
            </Button>
          </SidePaneFooter>
        )}
      </SidePane>

      {user && (
        <ChangeUserRoleDialog user={user} isOpen={isChangeRoleDialogOpen} onOpenChange={setIsChangeRoleDialogOpen} />
      )}
    </>
  );
}
