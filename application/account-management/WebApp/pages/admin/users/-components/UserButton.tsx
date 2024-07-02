import { Button } from "@repo/ui/components/Button";
import { Menu, MenuItem, MenuSeparator } from "@repo/ui/components/Menu";
import { useState } from "react";
import { MenuTrigger } from "react-aria-components";
import { Modal } from "@repo/ui/components/Modal";
import { AlertDialog } from "@repo/ui/components/AlertDialog";
import avatarUrl from "../../images/avatar.png";
import ProfileMenuItem from "./profileMenuItem";
import { LogOutIcon, SettingsIcon, UserIcon } from "lucide-react";

export function UserButton() {
  const [isProfileModalOpen, setIsProfileModalOpen] = useState(false);
  const [isAccountModalOpen, setIsAccountModalOpen] = useState(false);
  const [isSignOutModalOpen, setIsSignOutModalOpen] = useState(false);

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
            <LogOutIcon size={16} /> Sign out
          </MenuItem>
        </Menu>
      </MenuTrigger>

      {/* Profile */}
      <Modal isOpen={isProfileModalOpen} onOpenChange={setIsProfileModalOpen} isDismissable>
        <AlertDialog
        variant="info"
        actionLabel="Save changes"
        title="Profile Settings"
        onAction={() => setIsAccountModalOpen(false)}>
          Profile settings and options.
        </AlertDialog>
      </Modal>

      {/* Account Modal */}
      <Modal isOpen={isAccountModalOpen} onOpenChange={setIsAccountModalOpen} isDismissable>
        <AlertDialog
        variant="info"
        actionLabel="Save changes"
        title="Account Settings"
        onAction={() => setIsAccountModalOpen(false)}>
          Account settings and options.
        </AlertDialog>
      </Modal>

      {/* Sign Out Modal */}
      <Modal isOpen={isSignOutModalOpen} onOpenChange={setIsSignOutModalOpen} isDismissable>
        <AlertDialog
          variant="destructive"
          actionLabel="Confirm"
          title="Sign Out"
          onAction={() => setIsAccountModalOpen(false)}>
          Are you sure you want to sign out?
        </AlertDialog>
      </Modal>
    </>
  );
}
