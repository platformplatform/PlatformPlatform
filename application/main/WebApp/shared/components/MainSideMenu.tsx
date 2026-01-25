import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { collapsedContext, MenuButton, SideMenu, SideMenuSeparator } from "@repo/ui/components/SideMenu";
import AccountMenu from "account/AccountMenu";
import MobileMenu from "account/MobileMenu";
import { HomeIcon } from "lucide-react";
import { useContext } from "react";

function LogoContent() {
  const isCollapsed = useContext(collapsedContext);
  return <AccountMenu isCollapsed={isCollapsed} />;
}

export function MainSideMenu() {
  return (
    <SideMenu
      sidebarToggleAriaLabel={t`Toggle sidebar`}
      mobileMenuAriaLabel={t`Open navigation menu`}
      topMenuContent={<MobileMenu />}
      logoContent={<LogoContent />}
    >
      <SideMenuSeparator>
        <Trans>Navigation</Trans>
      </SideMenuSeparator>

      <MenuButton icon={HomeIcon} label={t`Home`} href="/home" />
    </SideMenu>
  );
}
