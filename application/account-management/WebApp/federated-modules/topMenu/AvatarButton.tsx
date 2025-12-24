import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { authSyncService } from "@repo/infrastructure/auth/AuthSyncService";
import { loginPath } from "@repo/infrastructure/auth/constants";
import { useUserInfo } from "@repo/infrastructure/auth/hooks";
import { createLoginUrlWithReturnPath } from "@repo/infrastructure/auth/util";
import { Avatar } from "@repo/ui/components/Avatar";
import { Button } from "@repo/ui/components/Button";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuGroup,
  DropdownMenuItem,
  DropdownMenuLabel,
  DropdownMenuSeparator,
  DropdownMenuTrigger
} from "@repo/ui/components/DropdownMenu";
import { useQueryClient } from "@tanstack/react-query";
import { LogOutIcon, MonitorSmartphoneIcon, UserIcon } from "lucide-react";
import { useEffect, useState } from "react";
import { api } from "@/shared/lib/api/client";
import SessionsModal from "../common/SessionsModal";
import UserProfileModal from "../common/UserProfileModal";
import "@repo/ui/tailwind.css";

export default function AvatarButton() {
  const [isProfileModalOpen, setIsProfileModalOpen] = useState(false);
  const [isSessionsModalOpen, setIsSessionsModalOpen] = useState(false);
  const [hasAutoOpenedModal, setHasAutoOpenedModal] = useState(false);
  const userInfo = useUserInfo();
  const queryClient = useQueryClient();

  useEffect(() => {
    // Only auto-open the modal once per session if user lacks firstName or lastName
    if (
      userInfo?.isAuthenticated &&
      (!userInfo.firstName || !userInfo.lastName) &&
      !hasAutoOpenedModal &&
      !isProfileModalOpen
    ) {
      setIsProfileModalOpen(true);
      setHasAutoOpenedModal(true);
    }
  }, [userInfo, hasAutoOpenedModal, isProfileModalOpen]);

  const logoutMutation = api.useMutation("post", "/api/account-management/authentication/logout", {
    onMutate: async () => {
      // Cancel all ongoing queries and remove them from cache to prevent 401 errors
      await queryClient.cancelQueries();
      queryClient.clear();
      setHasAutoOpenedModal(false); // Reset for clean state
    },
    onSuccess: () => {
      // Broadcast logout event to other tabs
      authSyncService.broadcast({
        type: "USER_LOGGED_OUT",
        userId: userInfo?.id || ""
      });

      window.location.href = createLoginUrlWithReturnPath(loginPath);
    },
    meta: {
      skipQueryInvalidation: true
    }
  });

  if (!userInfo) {
    return null;
  }

  return (
    <>
      <DropdownMenu>
        <DropdownMenuTrigger
          render={
            <Button aria-label={t`User profile menu`} variant="ghost" size="icon-lg" className="rounded-full">
              <Avatar avatarUrl={userInfo.avatarUrl} initials={userInfo.initials} isRound={true} size="sm" />
            </Button>
          }
        />
        <DropdownMenuContent align="end">
          <DropdownMenuGroup>
            <DropdownMenuLabel className="font-normal">
              <div className="flex flex-row items-center gap-2">
                <Avatar avatarUrl={userInfo.avatarUrl} initials={userInfo.initials ?? ""} isRound={true} size="sm" />
                <div className="my-1 flex flex-col">
                  <h4>{userInfo.fullName}</h4>
                  <p className="text-muted-foreground text-xs">{userInfo.email}</p>
                </div>
              </div>
            </DropdownMenuLabel>
          </DropdownMenuGroup>
          <DropdownMenuSeparator />
          <DropdownMenuItem onClick={() => setIsProfileModalOpen(true)}>
            <UserIcon size={16} />
            <Trans>Edit profile</Trans>
          </DropdownMenuItem>
          <DropdownMenuItem onClick={() => setIsSessionsModalOpen(true)}>
            <MonitorSmartphoneIcon size={16} />
            <Trans>Sessions</Trans>
          </DropdownMenuItem>
          <DropdownMenuSeparator />
          <DropdownMenuItem onClick={() => logoutMutation.mutate({})}>
            <LogOutIcon size={16} /> <Trans>Log out</Trans>
          </DropdownMenuItem>
        </DropdownMenuContent>
      </DropdownMenu>

      <UserProfileModal isOpen={isProfileModalOpen} onOpenChange={setIsProfileModalOpen} />
      {isSessionsModalOpen && <SessionsModal isOpen={isSessionsModalOpen} onOpenChange={setIsSessionsModalOpen} />}
    </>
  );
}
