import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { useUserInfo } from "@repo/infrastructure/auth/hooks";
import { collapsedContext, MenuButton, SideMenu, SideMenuSeparator } from "@repo/ui/components/SideMenu";
import { ArrowLeftIcon, CircleUserIcon, CreditCardIcon, HomeIcon, UsersIcon } from "lucide-react";
import { useContext } from "react";
import AccountMenu from "@/federated-modules/accountMenu/AccountMenu";
import MobileMenu from "@/federated-modules/sideMenu/MobileMenu";

function LogoContent() {
  const isCollapsed = useContext(collapsedContext);
  return <AccountMenu isCollapsed={isCollapsed} />;
}

function AccountNavigationMenuItems() {
  const userInfo = useUserInfo();

  return (
    <>
      <MenuButton icon={ArrowLeftIcon} label={t`Back to app`} href="/home" forceReload={true} />

      <SideMenuSeparator>
        <Trans>Account</Trans>
      </SideMenuSeparator>

      <MenuButton icon={HomeIcon} label={t`Home`} href="/account" />
      <MenuButton icon={CircleUserIcon} label={t`Settings`} href="/account/settings" />
      <MenuButton icon={UsersIcon} label={t`Users`} href="/account/users" />
      {userInfo?.role === "Owner" && (
        <MenuButton icon={CreditCardIcon} label={t`Subscription`} href="/account/subscription" />
      )}
    </>
  );
}

export function AccountSideMenu() {
  return (
    <SideMenu
      sidebarToggleAriaLabel={t`Toggle sidebar`}
      mobileMenuAriaLabel={t`Open navigation menu`}
      topMenuContent={<MobileMenu navigationContent={<AccountNavigationMenuItems />} />}
      logoContent={<LogoContent />}
    >
      <AccountNavigationMenuItems />
    </SideMenu>
  );
}
