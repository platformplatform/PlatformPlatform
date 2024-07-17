import { Button } from "@repo/ui/components/Button";
import { Menu, MenuItem, MenuSeparator } from "@repo/ui/components/Menu";
import { useState } from "react";
import { MenuTrigger } from "react-aria-components";
import avatarUrl from "../../images/avatar.png";
import AvatarMenuItem from "./AvatarMenuItem";
import { LogOutIcon, SettingsIcon, UserIcon } from "lucide-react";
import AccountModal from "./UserModals/AccountModal";
import UserProfileModal from "./UserModals/UserProfileModal";
import DeleteAccountModal from "./UserModals/DeleteAccountModal";

export function UserAvatarButton() {
  const [isProfileModalOpen, setIsProfileModalOpen] = useState(false);
  const [isAccountModalOpen, setIsAccountModalOpen] = useState(false);
  const [isSignOutModalOpen, setIsSignOutModalOpen] = useState(false);
  const [isDeleteAccountModalOpen, setIsDeleteAccountModalOpen] = useState(false);

  return (
    <>
      <MenuTrigger aria-label="account settings">
        <Button aria-label="Menu" variant="icon" className="w-12 h-12 rounded-full bg-transparent">
          <img src={avatarUrl} alt="Profile menu" />
        </Button>
        <Menu>
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
          <MenuItem onAction={() => setIsSignOutModalOpen(true)}>
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