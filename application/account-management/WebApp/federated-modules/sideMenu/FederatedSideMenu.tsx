import { t } from "@lingui/core/macro";
import { useUserInfo } from "@repo/infrastructure/auth/hooks";
import { SideMenu } from "@repo/ui/components/SideMenu";
import { MobileMenu } from "./MobileMenu";
import { NavigationMenuItems } from "./NavigationMenuItems";
import "@repo/ui/tailwind.css";

export type FederatedSideMenuProps = {
  currentSystem: "account-management" | "back-office"; // Add your self-contained system here
};

export default function FederatedSideMenu({ currentSystem }: Readonly<FederatedSideMenuProps>) {
  const userInfo = useUserInfo();

  return (
    <SideMenu
      ariaLabel={t`Toggle collapsed menu`}
      topMenuContent={<MobileMenu currentSystem={currentSystem} />}
      tenantName={userInfo?.tenantName}
    >
      <NavigationMenuItems currentSystem={currentSystem} />
    </SideMenu>
  );
}
