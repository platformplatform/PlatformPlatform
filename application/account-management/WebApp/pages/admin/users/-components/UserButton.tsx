import { Button } from "@repo/ui/components/Button";
import { Menu, MenuItem, MenuSeparator } from "@repo/ui/components/Menu";
import { useState } from "react";
import { MenuTrigger } from "react-aria-components";
import avatarUrl from "../../images/avatar.png";
import ProfileMenuItem from "./profileMenuItem";
import { LogOutIcon, SettingsIcon, UserIcon } from "lucide-react";
import AccountModal from "./UserModals/AccountModal";
import ProfileModal from "./UserModals/ProfileModal";
import DeleteAccountModal from "./UserModals/DeleteAccountModal";

export function UserButton() {
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
            <ProfileMenuItem />
          </MenuItem>
          <MenuSeparator />
          <MenuItem id="profile" onAction={() => setIsProfileModalOpen(true)}>
            <UserIcon size={16} />
            Profile
          </MenuItem>
          <MenuItem id="account" onAction={() => setIsAccountModalOpen(true)}>
            <SettingsIcon size={16} />
            Account
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
      <ProfileModal isOpen={isProfileModalOpen} onOpenChange={setIsProfileModalOpen} />
      <DeleteAccountModal isOpen={isDeleteAccountModalOpen} onOpenChange={setIsDeleteAccountModalOpen} />
    </>
  );
}
