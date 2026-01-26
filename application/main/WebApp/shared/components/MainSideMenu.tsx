import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { collapsedContext, MenuButton, SideMenu, SideMenuSeparator } from "@repo/ui/components/SideMenu";
import { useNavigate } from "@tanstack/react-router";
import AccountMenu from "account/AccountMenu";
import MobileMenu from "account/MobileMenu";
import { HomeIcon } from "lucide-react";
import { useContext } from "react";

function LogoContent({ onNavigate }: { onNavigate: (path: string) => void }) {
  const isCollapsed = useContext(collapsedContext);
  return <AccountMenu isCollapsed={isCollapsed} onNavigate={onNavigate} />;
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
      logoContent={<LogoContent onNavigate={handleNavigate} />}
    >
      <SideMenuSeparator>
        <Trans>Navigation</Trans>
      </SideMenuSeparator>

      <MenuButton icon={HomeIcon} label={t`Home`} href="/home" />
    </SideMenu>
  );
}
