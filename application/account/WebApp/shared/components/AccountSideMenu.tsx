import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { useUserInfo } from "@repo/infrastructure/auth/hooks";
import { collapsedContext, MenuButton, SideMenu, SideMenuSeparator } from "@repo/ui/components/SideMenu";
import {
  Building2Icon,
  CreditCardIcon,
  HomeIcon,
  MonitorSmartphoneIcon,
  SlidersHorizontalIcon,
  UserIcon,
  UsersIcon
} from "lucide-react";
import { useContext } from "react";
import MobileMenu from "@/federated-modules/sideMenu/MobileMenu";
import UserMenu from "@/federated-modules/userMenu/UserMenu";
import { useMainNavigation } from "@/shared/hooks/useMainNavigation";

function LogoContent() {
  const isCollapsed = useContext(collapsedContext);
  return <UserMenu isCollapsed={isCollapsed} />;
}

function AccountNavigationMenuItems() {
  const userInfo = useUserInfo();

  return (
    <>
      <SideMenuSeparator>
        <Trans>User</Trans>
      </SideMenuSeparator>

      <MenuButton icon={UserIcon} label={t`Profile`} ariaLabel={t`User profile`} href="/user/profile" />
      <MenuButton
        icon={SlidersHorizontalIcon}
        label={t`Preferences`}
        ariaLabel={t`User preferences`}
        href="/user/preferences"
      />
      <MenuButton icon={MonitorSmartphoneIcon} label={t`Sessions`} ariaLabel={t`User sessions`} href="/user/sessions" />

      <SideMenuSeparator>
        <Trans>Account</Trans>
      </SideMenuSeparator>

      <MenuButton icon={HomeIcon} label={t`Overview`} ariaLabel={t`Account overview`} href="/account" />
      <MenuButton icon={Building2Icon} label={t`Settings`} ariaLabel={t`Account settings`} href="/account/settings" />
      <MenuButton icon={UsersIcon} label={t`Users`} href="/account/users" matchPrefix={true} />
      {userInfo?.role === "Owner" && import.meta.runtime_env.PUBLIC_SUBSCRIPTION_ENABLED === "true" && (
        <MenuButton icon={CreditCardIcon} label={t`Subscription`} href="/account/subscription" matchPrefix={true} />
      )}
    </>
  );
}

export function AccountSideMenu() {
  const { navigateToMain } = useMainNavigation();

  return (
    <SideMenu
      sidebarToggleAriaLabel={t`Toggle sidebar`}
      mobileMenuAriaLabel={t`Open navigation menu`}
      topMenuContent={<MobileMenu onNavigate={navigateToMain} />}
      logoContent={<LogoContent />}
    >
      <AccountNavigationMenuItems />
    </SideMenu>
  );
}
