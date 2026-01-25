import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { useUserInfo } from "@repo/infrastructure/auth/hooks";
import { FederatedMenuButton, SideMenuSeparator } from "@repo/ui/components/SideMenu";
import { BoxIcon, CircleUserIcon, HomeIcon, MonitorSmartphoneIcon, UserIcon, UsersIcon } from "lucide-react";
import type { FederatedSideMenuProps } from "./FederatedSideMenu";

// Navigation items shared between mobile and desktop menus
export function NavigationMenuItems({
  currentSystem
}: Readonly<{ currentSystem: FederatedSideMenuProps["currentSystem"] }>) {
  const userInfo = useUserInfo();

  return (
    <>
      <FederatedMenuButton
        icon={HomeIcon}
        label={t`Home`}
        href="/account"
        isCurrentSystem={currentSystem === "account"}
      />

      <SideMenuSeparator>
        <Trans>User</Trans>
      </SideMenuSeparator>

      <FederatedMenuButton
        icon={UserIcon}
        label={t`Profile`}
        href="/account/profile"
        isCurrentSystem={currentSystem === "account"}
      />
      <FederatedMenuButton
        icon={MonitorSmartphoneIcon}
        label={t`Sessions`}
        href="/account/sessions"
        isCurrentSystem={currentSystem === "account"}
      />

      <SideMenuSeparator>
        <Trans>Organization</Trans>
      </SideMenuSeparator>

      <FederatedMenuButton
        icon={CircleUserIcon}
        label={t`Account`}
        href="/account/settings"
        isCurrentSystem={currentSystem === "account"}
      />
      <FederatedMenuButton
        icon={UsersIcon}
        label={t`Users`}
        href="/account/users"
        isCurrentSystem={currentSystem === "account"}
      />

      {userInfo?.isInternalUser && (
        <>
          <SideMenuSeparator>
            <Trans>Back Office</Trans>
          </SideMenuSeparator>

          <FederatedMenuButton
            icon={BoxIcon}
            label={t`Dashboard`}
            href="/back-office"
            isCurrentSystem={currentSystem === "back-office"}
          />
        </>
      )}
    </>
  );
}
