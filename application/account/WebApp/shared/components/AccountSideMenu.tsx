import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { collapsedContext, MenuButton, SideMenu, SideMenuSeparator } from "@repo/ui/components/SideMenu";
import { CircleUserIcon, HomeIcon, MonitorSmartphoneIcon, UserIcon, UsersIcon } from "lucide-react";
import { useContext } from "react";
import AccountMenu from "@/federated-modules/accountMenu/AccountMenu";
import MobileMenu from "@/federated-modules/sideMenu/MobileMenu";
import { useMainNavigation } from "@/shared/hooks/useMainNavigation";

function LogoContent() {
  const isCollapsed = useContext(collapsedContext);
  return <AccountMenu isCollapsed={isCollapsed} />;
}

function AccountNavigationMenuItems() {
  return (
    <>
      <SideMenuSeparator>
        <Trans>User</Trans>
      </SideMenuSeparator>

      <MenuButton icon={UserIcon} label={t`Profile`} href="/user/profile" />
      <MenuButton icon={MonitorSmartphoneIcon} label={t`Sessions`} href="/user/sessions" />

      <SideMenuSeparator>
        <Trans>Account</Trans>
      </SideMenuSeparator>

      <MenuButton icon={HomeIcon} label={t`Overview`} href="/account" />
      <MenuButton icon={CircleUserIcon} label={t`Settings`} href="/account/settings" />
      <MenuButton icon={UsersIcon} label={t`Users`} href="/account/users" matchPrefix={true} />
    </>
  );
}

export function AccountSideMenu() {
  const { navigateToMain } = useMainNavigation();

  return (
    <SideMenu
      sidebarToggleAriaLabel={t`Toggle sidebar`}
      mobileMenuAriaLabel={t`Open navigation menu`}
      topMenuContent={<MobileMenu navigationContent={<AccountNavigationMenuItems />} onNavigate={navigateToMain} />}
      logoContent={<LogoContent />}
    >
      <AccountNavigationMenuItems />
    </SideMenu>
  );
}
