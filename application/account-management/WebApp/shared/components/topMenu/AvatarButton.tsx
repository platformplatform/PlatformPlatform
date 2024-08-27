import { Button } from "@repo/ui/components/Button";
import { Menu, MenuHeader, MenuItem, MenuSeparator, MenuTrigger } from "@repo/ui/components/Menu";
import { useState } from "react";
import { AvatarMenuItem } from "./AvatarMenuItem";
import { LogOutIcon, SettingsIcon, UserIcon } from "lucide-react";
import AccountModal from "@/shared/components/accountModals/AccountSettingsModal";
import UserProfileModal from "@/shared/components/userModals/UserProfileModal";
import DeleteAccountModal from "@/shared/components/accountModals/DeleteAccountConfirmation";
import { Avatar } from "@repo/ui/components/Avatar";
import { useUserInfo } from "@repo/infrastructure/auth/hooks";
import { api } from "@/shared/lib/api/client";

export function AvatarButton() {
  const [isProfileModalOpen, setIsProfileModalOpen] = useState(false);
  const [isAccountModalOpen, setIsAccountModalOpen] = useState(false);
  const [isDeleteAccountModalOpen, setIsDeleteAccountModalOpen] = useState(false);
  const userInfo = useUserInfo();

  if (!userInfo) return null;

  async function logut() {
    await api.post("/api/account-management/authentication/logout");
    window.location.reload();
  }

  return (
    <>
      <MenuTrigger aria-label="account settings">
        <Button aria-label="Menu" variant="icon" className="rounded-full">
          <Avatar avatarUrl={userInfo.avatarUrl} initials={userInfo.initials} isRound size="sm" />
        </Button>
        <Menu placement="bottom end">
          <MenuHeader>
            <AvatarMenuItem
              name={userInfo.fullName}
              title={userInfo.title}
              email={userInfo.email}
              avatarUrl={userInfo.avatarUrl}
              initials={userInfo.initials}
            />
          </MenuHeader>
          <MenuSeparator />
          <MenuItem id="profile" onAction={() => setIsProfileModalOpen(true)}>
            <UserIcon size={16} />
            Edit profile
          </MenuItem>
          <MenuItem id="account" onAction={() => setIsAccountModalOpen(true)}>
            <SettingsIcon size={16} />
            Account settings
          </MenuItem>
          <MenuSeparator />
          <MenuItem id="logout" onAction={logut}>
            <LogOutIcon size={16} /> Log out
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
    </>
  );
}
