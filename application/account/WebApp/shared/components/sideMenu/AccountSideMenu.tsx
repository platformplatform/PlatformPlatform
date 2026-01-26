import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { useUserInfo } from "@repo/infrastructure/auth/hooks";
import { collapsedContext, MenuButton, SideMenu, SideMenuSeparator } from "@repo/ui/components/SideMenu";
import {
  ArrowLeftIcon,
  CircleUserIcon,
  CreditCardIcon,
  HomeIcon,
  MonitorSmartphoneIcon,
  UserIcon,
  UsersIcon
} from "lucide-react";
import { useContext } from "react";
import AccountMenu from "@/federated-modules/accountMenu/AccountMenu";
import MobileMenu from "@/federated-modules/sideMenu/MobileMenu";
import { useMainNavigation } from "@/shared/hooks/useMainNavigation";

function LogoContent({ onNavigate }: { onNavigate: (path: string) => void }) {
  const isCollapsed = useContext(collapsedContext);
  return <AccountMenu isCollapsed={isCollapsed} onNavigate={onNavigate} />;
}

function AccountNavigationMenuItems({ onNavigate }: { onNavigate: (path: string) => void }) {
  const userInfo = useUserInfo();

  return (
    <>
      <MenuButton
        icon={ArrowLeftIcon}
        label={t`Back to app`}
        href="/home"
        federatedNavigation={true}
        onNavigate={onNavigate}
      />

      <SideMenuSeparator>
        <Trans>User</Trans>
      </SideMenuSeparator>

      <MenuButton icon={UserIcon} label={t`Profile`} href="/account/profile" />
      <MenuButton icon={MonitorSmartphoneIcon} label={t`Sessions`} href="/account/sessions" />

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
  const { navigateToMain } = useMainNavigation();

  return (
    <SideMenu
      sidebarToggleAriaLabel={t`Toggle sidebar`}
      mobileMenuAriaLabel={t`Open navigation menu`}
      topMenuContent={
        <MobileMenu
          navigationContent={<AccountNavigationMenuItems onNavigate={navigateToMain} />}
          onNavigate={navigateToMain}
        />
      }
      logoContent={<LogoContent onNavigate={navigateToMain} />}
    >
      <AccountNavigationMenuItems onNavigate={navigateToMain} />
    </SideMenu>
  );
}
