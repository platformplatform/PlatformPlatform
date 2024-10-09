import { MenuButton, MenuSeparator, SideMenu } from "@repo/ui/components/SideMenu";
import { CircleUserIcon, HomeIcon, UsersIcon } from "lucide-react";
import { t, Trans } from "@lingui/macro";

type SharedSideMenuProps = {
  children?: React.ReactNode;
};

export function SharedSideMenu({ children }: Readonly<SharedSideMenuProps>) {
  return (
    <SideMenu>
      <MenuButton icon={HomeIcon} label={t`Home`} href="/admin" />
      <MenuSeparator>
        <Trans>Organization</Trans>
      </MenuSeparator>
      <MenuButton icon={CircleUserIcon} label={t`Account`} href="/admin/account" />
      <MenuButton icon={UsersIcon} label={t`Users`} href="/admin/users" />
      {children}
    </SideMenu>
  );
}
