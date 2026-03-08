import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { useUserInfo } from "@repo/infrastructure/auth/hooks";
import { Button } from "@repo/ui/components/Button";
import { SidePane, SidePaneBody, SidePaneFooter, SidePaneHeader } from "@repo/ui/components/SidePane";
import { Skeleton } from "@repo/ui/components/Skeleton";
import { Tooltip, TooltipContent, TooltipTrigger } from "@repo/ui/components/Tooltip";
import { InfoIcon, Trash2Icon } from "lucide-react";
import { useState } from "react";

import type { components } from "@/shared/lib/api/client";

import { ChangeUserRoleDialog } from "./ChangeUserRoleDialog";
import { UserProfileContent } from "./UserProfileContent";

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

function NoticeBar({ children }: Readonly<{ children: React.ReactNode }>) {
  return (
    <div className="border-b border-border bg-muted px-4 py-3">
      <div className="flex items-center gap-2 text-muted-foreground">
        <InfoIcon className="size-4 flex-shrink-0" />
        <p className="text-sm font-medium">{children}</p>
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
      <SidePane
        isOpen={isOpen}
        onOpenChange={(open) => !open && onClose()}
        trackingTitle="User profile"
        trackingKey={user?.id}
        aria-label={t`User profile`}
      >
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
                isCurrentUser={isCurrentUser ?? false}
                onChangeRole={() => setIsChangeRoleDialogOpen(true)}
              />
            )
          )}
        </SidePaneBody>

        {userInfo?.role === "Owner" && user && (
          <SidePaneFooter>
            {isCurrentUser ? (
              <Tooltip>
                <TooltipTrigger render={<span className="inline-block w-full" />}>
                  <Button
                    variant="destructive"
                    className="pointer-events-none w-full justify-center text-sm"
                    disabled={true}
                  >
                    <Trash2Icon className="size-4" />
                    <Trans>Delete user</Trans>
                  </Button>
                </TooltipTrigger>
                <TooltipContent>{t`You cannot delete yourself`}</TooltipContent>
              </Tooltip>
            ) : (
              <Button
                variant="destructive"
                onClick={() => onDeleteUser(user)}
                className="w-full justify-center text-sm"
              >
                <Trash2Icon className="size-4" />
                <Trans>Delete user</Trans>
              </Button>
            )}
          </SidePaneFooter>
        )}
      </SidePane>

      {user && (
        <ChangeUserRoleDialog user={user} isOpen={isChangeRoleDialogOpen} onOpenChange={setIsChangeRoleDialogOpen} />
      )}
    </>
  );
}
