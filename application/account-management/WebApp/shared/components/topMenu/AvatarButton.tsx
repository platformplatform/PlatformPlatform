import { Button } from "@repo/ui/components/Button";
import { Menu, MenuItem, MenuSeparator, MenuTrigger } from "@repo/ui/components/Menu";
import { useState } from "react";
import avatarUrl from "./images/avatar.png";
import AvatarMenuItem from "./AvatarMenuItem";
import { LogOutIcon, SettingsIcon, UserIcon } from "lucide-react";
import AccountModal from "@/shared/components/accountModals/AccountSettingsModal";
import UserProfileModal from "@/shared/components/userModals/UserProfileModal";
import DeleteAccountModal from "@/shared/components/accountModals/DeleteAccountConfirmation";
import { Avatar } from "@repo/ui/components/Avatar";

export function AvatarButton() {
  const [isProfileModalOpen, setIsProfileModalOpen] = useState(false);
  const [isAccountModalOpen, setIsAccountModalOpen] = useState(false);
  const [isDeleteAccountModalOpen, setIsDeleteAccountModalOpen] = useState(false);

  return (
    <>
      <MenuTrigger aria-label="account settings">
        <Button aria-label="Menu" variant="icon" className="rounded-full">
          <Avatar avatarUrl={avatarUrl} initials="MD" isRound size="sm" />
        </Button>
        <Menu placement="bottom end">
          <MenuItem className="h-16 w-60" onAction={() => setIsProfileModalOpen(true)}>
            <AvatarMenuItem />
          </MenuItem>
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
          <MenuItem href="/">
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
      <UserProfileModal isOpen={isProfileModalOpen} onOpenChange={setIsProfileModalOpen} />
      <DeleteAccountModal isOpen={isDeleteAccountModalOpen} onOpenChange={setIsDeleteAccountModalOpen} />
    </>
  );
}
