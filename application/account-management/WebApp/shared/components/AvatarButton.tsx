import { Button } from "@repo/ui/components/Button";
import { Menu, MenuHeader, MenuItem, MenuSeparator, MenuTrigger } from "@repo/ui/components/Menu";
import { useEffect, useState } from "react";
import { LogOutIcon, UserIcon } from "lucide-react";
import UserProfileModal from "@/shared/components/userModals/UserProfileModal";
import { Avatar } from "@repo/ui/components/Avatar";
import { useUserInfo } from "@repo/infrastructure/auth/hooks";
import { api } from "@/shared/lib/api/client";
import { Trans } from "@lingui/react/macro";
import { createLoginUrlWithReturnPath } from "@repo/infrastructure/auth/util";
import { loginPath } from "@repo/infrastructure/auth/constants";

export default function AvatarButton({ "aria-label": ariaLabel }: { "aria-label": string }) {
  const [isProfileModalOpen, setIsProfileModalOpen] = useState(false);
  const userInfo = useUserInfo();

  useEffect(() => {
    if (userInfo?.isAuthenticated && (!userInfo.firstName || !userInfo.lastName)) {
      setIsProfileModalOpen(true);
    }
  }, [userInfo]);

  if (!userInfo) return null;

  const logoutMutation = api.useMutation("post", "/api/account-management/authentication/logout");

  async function logout() {
    await logoutMutation.mutateAsync({});
    window.location.href = createLoginUrlWithReturnPath(loginPath);
  }

  return (
    <>
      <MenuTrigger>
        <Button aria-label={ariaLabel} variant="icon" className="rounded-full">
          <Avatar avatarUrl={userInfo.avatarUrl} initials={userInfo.initials} isRound size="sm" />
        </Button>
        <Menu placement="bottom end">
          <MenuHeader>
            <div className="flex flex-row items-center gap-2">
              <Avatar avatarUrl={userInfo.avatarUrl} initials={userInfo.initials ?? ""} isRound size="sm" />
              <div className="flex flex-col my-1">
                <h2>{userInfo.fullName}</h2>
                <p className="text-muted-foreground">{userInfo.title ?? userInfo.email}</p>
              </div>
            </div>
          </MenuHeader>
          <MenuSeparator />
          <MenuItem id="profile" onAction={() => setIsProfileModalOpen(true)}>
            <UserIcon size={16} />
            <Trans>Edit profile</Trans>
          </MenuItem>
          <MenuSeparator />
          <MenuItem id="logout" onAction={logout}>
            <LogOutIcon size={16} /> <Trans>Log out</Trans>
          </MenuItem>
        </Menu>
      </MenuTrigger>

      <UserProfileModal isOpen={isProfileModalOpen} onOpenChange={setIsProfileModalOpen} userId={userInfo.id ?? ""} />
    </>
  );
}
