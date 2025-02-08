import type React from "react";
import { MenuButton, SideMenu, SideMenuSeparator } from "@repo/ui/components/SideMenu";
import { CircleUserIcon, HomeIcon, UsersIcon } from "lucide-react";
import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";

type SharedSideMenuProps = {
  children?: React.ReactNode;
  ariaLabel: string;
};

export function SharedSideMenu({ children, ariaLabel }: Readonly<SharedSideMenuProps>) {
  return (
    <SideMenu ariaLabel={ariaLabel}>
      <MenuButton icon={HomeIcon} label={t`Home`} href="/admin" />
      <SideMenuSeparator>
        <Trans>Organization</Trans>
      </SideMenuSeparator>
      <MenuButton icon={CircleUserIcon} label={t`Account`} href="/admin/account" forceReload />
      <MenuButton icon={UsersIcon} label={t`Users`} href="/admin/users" />
      {children}
    </SideMenu>
  );
}
