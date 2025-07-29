import { t } from "@lingui/core/macro";
import { useUserInfo } from "@repo/infrastructure/auth/hooks";
import { MenuButton, SideMenu, SideMenuSpacer } from "@repo/ui/components/SideMenu";
import { BoxIcon, HomeIcon } from "lucide-react";
import type React from "react";

type SharedSideMenuProps = {
  children?: React.ReactNode;
  ariaLabel: string;
};

export function SharedSideMenu({ children, ariaLabel }: Readonly<SharedSideMenuProps>) {
  const userInfo = useUserInfo();

  return (
    <SideMenu ariaLabel={ariaLabel} tenantName={userInfo?.tenantName}>
      <MenuButton icon={HomeIcon} label={t`Home`} href="/back-office" />
      {children}

      <SideMenuSpacer />

      <MenuButton icon={BoxIcon} label={t`Account management`} href="/admin" forceReload={true} />
    </SideMenu>
  );
}
