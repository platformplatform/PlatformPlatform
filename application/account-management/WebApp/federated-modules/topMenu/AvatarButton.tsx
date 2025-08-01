import { api } from "@/shared/lib/api/client";
import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { authSyncService } from "@repo/infrastructure/auth/AuthSyncService";
import { loginPath } from "@repo/infrastructure/auth/constants";
import { useUserInfo } from "@repo/infrastructure/auth/hooks";
import { createLoginUrlWithReturnPath } from "@repo/infrastructure/auth/util";
import { Avatar } from "@repo/ui/components/Avatar";
import { Button } from "@repo/ui/components/Button";
import { Menu, MenuHeader, MenuItem, MenuSeparator, MenuTrigger } from "@repo/ui/components/Menu";
import { useQueryClient } from "@tanstack/react-query";
import { LogOutIcon, UserIcon } from "lucide-react";
import { useEffect, useState } from "react";
import UserProfileModal from "../common/UserProfileModal";
import "@repo/ui/tailwind.css";

export default function AvatarButton() {
  const [isProfileModalOpen, setIsProfileModalOpen] = useState(false);
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
        type: "USER_LOGGED_OUT"
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
      <MenuTrigger>
        <Button aria-label={t`User profile menu`} variant="icon" className="rounded-full">
          <Avatar avatarUrl={userInfo.avatarUrl} initials={userInfo.initials} isRound={true} size="sm" />
        </Button>
        <Menu placement="bottom end">
          <MenuHeader>
            <div className="flex flex-row items-center gap-2">
              <Avatar avatarUrl={userInfo.avatarUrl} initials={userInfo.initials ?? ""} isRound={true} size="sm" />
              <div className="my-1 flex flex-col">
                <h4>{userInfo.fullName}</h4>
                <p className="subtitle">{userInfo.email}</p>
              </div>
            </div>
          </MenuHeader>
          <MenuSeparator />
          <MenuItem id="profile" onAction={() => setIsProfileModalOpen(true)}>
            <UserIcon size={16} />
            <Trans>Edit profile</Trans>
          </MenuItem>
          <MenuSeparator />
          <MenuItem id="logout" onAction={() => logoutMutation.mutate({})}>
            <LogOutIcon size={16} /> <Trans>Log out</Trans>
          </MenuItem>
        </Menu>
      </MenuTrigger>

      <UserProfileModal isOpen={isProfileModalOpen} onOpenChange={setIsProfileModalOpen} />
    </>
  );
}
