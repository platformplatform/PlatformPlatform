import { MenuButton, SideMenu, SideMenuSeparator, SideMenuSpacer } from "@repo/ui/components/SideMenu";
import { BoxIcon, CircleUserIcon, HomeIcon, UsersIcon } from "lucide-react";
import { t, Trans } from "@lingui/macro";

type SharedSideMenuProps = {
  children?: React.ReactNode;
};

export function SharedSideMenu({ children }: Readonly<SharedSideMenuProps>) {
  return (
    <SideMenu>
      <MenuButton icon={HomeIcon} label={t`Home`} href="/admin" />
      <SideMenuSeparator>
        <Trans>Organization</Trans>
      </SideMenuSeparator>
      <MenuButton icon={CircleUserIcon} label={t`Account`} href="/admin/account" />
      <MenuButton icon={UsersIcon} label={t`Users`} href="/admin/users" />
      {children}

      <SideMenuSpacer />
      <MenuButton icon={BoxIcon} label={t`Back Office`} href="/back-office" serverNavigation />
    </SideMenu>
  );
}
