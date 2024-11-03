import type React from "react";
import { MenuButton, SideMenu, SideMenuSeparator } from "@repo/ui/components/SideMenu";
import { CircleUserIcon, HomeIcon, UsersIcon } from "lucide-react";
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
      <MenuButton icon={CircleUserIcon} label={t`Account`} href="/admin/account" isDisabled forceReload />
      <MenuButton icon={UsersIcon} label={t`Users`} href="/admin/users" />
      {children}
    </SideMenu>
  );
}
