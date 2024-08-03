import { Button } from "@repo/ui/components/Button";
import { Menu, MenuItem, MenuSeparator, MenuTrigger } from "@repo/ui/components/Menu";
import { useState } from "react";
import { LogOutIcon, SettingsIcon, UserIcon } from "lucide-react";
import AccountModal from "@/shared/components/accountModals/AccountSettingsModal";
import UserProfileModal from "@/shared/components/userModals/UserProfileModal";
import DeleteAccountModal from "@/shared/components/accountModals/DeleteAccountConfirmation";
import { Avatar, type AvatarProps } from "@repo/ui/components/Avatar";
import { useUserInfo } from "@repo/infrastructure/auth/hooks";

export type UserButtonProps = Pick<AvatarProps, "size" | "isRound">;

export function UserButton({ isRound = false, size = "xs" }: Readonly<UserButtonProps>) {
  const [isProfileModalOpen, setIsProfileModalOpen] = useState(false);
  const [isAccountModalOpen, setIsAccountModalOpen] = useState(false);
  const [isDeleteAccountModalOpen, setIsDeleteAccountModalOpen] = useState(false);
  const userInfo = useUserInfo();

  if (!userInfo) return null;

  return (
    <>
      <MenuTrigger aria-label="account settings">
        <Button aria-label="Menu" variant="icon" className={isRound ? "rounded-full" : "rounded-md"}>
          <Avatar avatarUrl={userInfo.avatarUrl} initials={userInfo.initials} isRound={isRound} size={size} />
        </Button>
        <Menu placement="bottom end">
          <MenuItem onAction={() => setIsProfileModalOpen(true)}>
            <div className="flex flex-row items-center gap-2">
              <Avatar avatarUrl={userInfo.avatarUrl} initials={userInfo.initials} isRound={isRound} size={size} />
              <div className="flex flex-col">
                <h2>{userInfo.fullName}</h2>
                <p className="text-muted-foreground text-sm font-normal">{userInfo.title}</p>
              </div>
            </div>
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
          <MenuItem onAction={() => {}}>
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

export default UserButton;
