import { Button } from "@repo/ui/components/Button";
import { Menu, MenuHeader, MenuItem, MenuSeparator, MenuTrigger } from "@repo/ui/components/Menu";
import { useEffect, useState } from "react";
import { LogOutIcon, SettingsIcon, UserIcon } from "lucide-react";
import AccountModal from "@/shared/components/accountModals/AccountSettingsModal";
import UserProfileModal from "@/shared/components/userModals/UserProfileModal";
import DeleteAccountModal from "@/shared/components/accountModals/DeleteAccountConfirmation";
import { Avatar } from "@repo/ui/components/Avatar";
import { useUserInfo } from "@repo/infrastructure/auth/hooks";
import { api } from "@/shared/lib/api/client";
import { t, Trans } from "@lingui/macro";

export default function AvatarButton() {
  const [isProfileModalOpen, setIsProfileModalOpen] = useState(false);
  const [isAccountModalOpen, setIsAccountModalOpen] = useState(false);
  const [isDeleteAccountModalOpen, setIsDeleteAccountModalOpen] = useState(false);
  const userInfo = useUserInfo();

  useEffect(() => {
    if (userInfo?.isAuthenticated && (!userInfo.firstName || !userInfo.lastName)) {
      setIsProfileModalOpen(true);
    }
  }, [userInfo]);

  if (!userInfo) return null;

  async function logout() {
    await api.post("/api/account-management/authentication/logout");
    window.location.reload();
  }

  return (
    <>
      <MenuTrigger aria-label={t`Account settings`}>
        <Button aria-label={t`Menu`} variant="icon" className="rounded-full">
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
          <MenuItem id="account" onAction={() => setIsAccountModalOpen(true)}>
            <SettingsIcon size={16} />
            <Trans>Account settings</Trans>
          </MenuItem>
          <MenuSeparator />
          <MenuItem id="logout" onAction={logout}>
            <LogOutIcon size={16} /> <Trans>Log out</Trans>
          </MenuItem>
        </Menu>
      </MenuTrigger>

      <AccountModal
        isOpen={isAccountModalOpen}
        onOpenChange={setIsAccountModalOpen}
        onDeleteAccount={() => {
          setIsAccountModalOpen(false);
          setIsDeleteAccountModalOpen(true);
        }}
      />
      <UserProfileModal
        isOpen={isProfileModalOpen}
        onOpenChange={setIsProfileModalOpen}
        userId={userInfo.userId ?? ""}
      />
      <DeleteAccountModal isOpen={isDeleteAccountModalOpen} onOpenChange={setIsDeleteAccountModalOpen} />

      {userInfo?.isAuthenticated && (
        <UserProfileModal
          isOpen={isProfileModalOpen}
          onOpenChange={setIsProfileModalOpen}
          userId={userInfo.userId ?? ""}
        />
      )}
    </>
  );
}
