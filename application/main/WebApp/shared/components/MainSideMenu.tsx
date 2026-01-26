import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { collapsedContext, MenuButton, SideMenu, SideMenuSeparator } from "@repo/ui/components/SideMenu";
import { useNavigate } from "@tanstack/react-router";
import AccountMenu from "account/AccountMenu";
import MobileMenu from "account/MobileMenu";
import { LayoutDashboardIcon } from "lucide-react";
import { useContext } from "react";

function LogoContent() {
  const isCollapsed = useContext(collapsedContext);
  return <AccountMenu isCollapsed={isCollapsed} />;
}

export function MainSideMenu() {
  const navigate = useNavigate();
  const handleNavigate = (path: string) => {
    navigate({ to: path });
  };

  return (
    <SideMenu
      sidebarToggleAriaLabel={t`Toggle sidebar`}
      mobileMenuAriaLabel={t`Open navigation menu`}
      topMenuContent={<MobileMenu onNavigate={handleNavigate} />}
      logoContent={<LogoContent />}
    >
      <SideMenuSeparator>
        <Trans>Navigation</Trans>
      </SideMenuSeparator>

      <MenuButton icon={LayoutDashboardIcon} label={t`Dashboard`} href="/dashboard" />
    </SideMenu>
  );
}
