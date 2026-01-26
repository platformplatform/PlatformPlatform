import { t } from "@lingui/core/macro";
import { collapsedContext, SideMenu } from "@repo/ui/components/SideMenu";
import { useContext } from "react";
import { useMainNavigation } from "@/shared/hooks/useMainNavigation";
import AccountMenu from "../accountMenu/AccountMenu";
import MobileMenu from "./MobileMenu";
import { NavigationMenuItems } from "./NavigationMenuItems";
import "@repo/ui/tailwind.css";

export type FederatedSideMenuProps = {
  currentSystem: "account" | "back-office";
};

function LogoContent() {
  const isCollapsed = useContext(collapsedContext);
  return <AccountMenu isCollapsed={isCollapsed} />;
}

export default function FederatedSideMenu({ currentSystem }: Readonly<FederatedSideMenuProps>) {
  const { navigateToMain } = useMainNavigation();

  return (
    <SideMenu
      sidebarToggleAriaLabel={t`Toggle sidebar`}
      mobileMenuAriaLabel={t`Open navigation menu`}
      topMenuContent={
        <MobileMenu
          navigationContent={<NavigationMenuItems currentSystem={currentSystem} onNavigate={navigateToMain} />}
          onNavigate={navigateToMain}
        />
      }
      logoContent={<LogoContent />}
    >
      <NavigationMenuItems currentSystem={currentSystem} onNavigate={navigateToMain} />
    </SideMenu>
  );
}
