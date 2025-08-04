import { t } from "@lingui/core/macro";
import { useUserInfo } from "@repo/infrastructure/auth/hooks";
import { SideMenu } from "@repo/ui/components/SideMenu";
import { useState } from "react";
import UserProfileModal from "../common/UserProfileModal";
import { MobileMenu } from "./MobileMenu";
import { NavigationMenuItems } from "./NavigationMenuItems";
import "@repo/ui/tailwind.css";

export type FederatedSideMenuProps = {
  currentSystem: "account-management" | "back-office"; // Add your self-contained system here
};

export default function FederatedSideMenu({ currentSystem }: Readonly<FederatedSideMenuProps>) {
  const userInfo = useUserInfo();
  const [isProfileModalOpen, setIsProfileModalOpen] = useState(false);

  return (
    <>
      <SideMenu
        sidebarToggleAriaLabel={t`Toggle sidebar`}
        mobileMenuAriaLabel={t`Open navigation menu`}
        topMenuContent={<MobileMenu currentSystem={currentSystem} onEditProfile={() => setIsProfileModalOpen(true)} />}
        tenantName={userInfo?.tenantName}
        tenantLogoUrl={userInfo?.tenantLogoUrl ?? undefined}
      >
        <NavigationMenuItems currentSystem={currentSystem} />
      </SideMenu>
      <UserProfileModal isOpen={isProfileModalOpen} onOpenChange={setIsProfileModalOpen} />
    </>
  );
}
