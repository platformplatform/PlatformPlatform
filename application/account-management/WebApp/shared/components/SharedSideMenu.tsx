import { MenuButton, MenuSeparator, SideMenu } from "@repo/ui/components/SideMenu";
import { CircleUserIcon, HomeIcon, UsersIcon } from "lucide-react";

type SharedSideMenuProps = {
  children?: React.ReactNode;
};

export function SharedSideMenu({ children }: Readonly<SharedSideMenuProps>) {
  return (
    <SideMenu>
      <MenuButton icon={HomeIcon} label="Home" href="/admin" />
      <MenuSeparator>Organisation</MenuSeparator>
      <MenuButton icon={CircleUserIcon} label="Account" href="/admin/account" />
      <MenuButton icon={UsersIcon} label="Users" href="/admin/users" />
      {children}
    </SideMenu>
  );
}
