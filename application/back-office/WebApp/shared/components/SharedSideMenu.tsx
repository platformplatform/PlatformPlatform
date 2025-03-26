import { t } from "@lingui/core/macro";
import { MenuButton, SideMenu, SideMenuSpacer } from "@repo/ui/components/SideMenu";
import { BoxIcon, HomeIcon } from "lucide-react";
import type React from "react";

type SharedSideMenuProps = {
  children?: React.ReactNode;
  ariaLabel: string;
};

export function SharedSideMenu({ children, ariaLabel }: Readonly<SharedSideMenuProps>) {
  return (
    <SideMenu ariaLabel={ariaLabel}>
      <MenuButton icon={HomeIcon} label={t`Home`} href="/back-office" />
      {children}

      <SideMenuSpacer />

      <MenuButton icon={BoxIcon} label={t`Account management`} href="/admin" forceReload={true} />
    </SideMenu>
  );
}
