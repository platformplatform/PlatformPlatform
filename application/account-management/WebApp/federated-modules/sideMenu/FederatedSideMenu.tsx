import { t } from "@lingui/core/macro";
import { SideMenu } from "@repo/ui/components/SideMenu";
import { useState } from "react";
import TenantSelector from "../common/TenantSelector";
import UserProfileModal from "../common/UserProfileModal";
import { MobileMenu } from "./MobileMenu";
import { NavigationMenuItems } from "./NavigationMenuItems";
import "@repo/ui/tailwind.css";

export type FederatedSideMenuProps = {
  currentSystem: "account-management" | "back-office"; // Add your self-contained system here
};

export default function FederatedSideMenu({ currentSystem }: Readonly<FederatedSideMenuProps>) {
  const [isProfileModalOpen, setIsProfileModalOpen] = useState(false);

  return (
    <>
      <SideMenu
        sidebarToggleAriaLabel={t`Toggle sidebar`}
        mobileMenuAriaLabel={t`Open navigation menu`}
        topMenuContent={<MobileMenu currentSystem={currentSystem} onEditProfile={() => setIsProfileModalOpen(true)} />}
        logoContent={<TenantSelector />}
      >
        <NavigationMenuItems currentSystem={currentSystem} />
      </SideMenu>
      <UserProfileModal isOpen={isProfileModalOpen} onOpenChange={setIsProfileModalOpen} />
    </>
  );
}
