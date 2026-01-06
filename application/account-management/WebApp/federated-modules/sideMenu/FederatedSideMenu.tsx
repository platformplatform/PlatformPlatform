import { t } from "@lingui/core/macro";
import { loggedInPath } from "@repo/infrastructure/auth/constants";
import { useUserInfo } from "@repo/infrastructure/auth/hooks";
import { SideMenu } from "@repo/ui/components/SideMenu";
import { useState } from "react";
import { useSwitchTenant } from "@/shared/hooks/useSwitchTenant";
import type { components } from "@/shared/lib/api/api.generated";
import { AcceptInvitationDialog } from "../common/AcceptInvitationDialog";
import SessionsModal from "../common/SessionsModal";
import { SwitchingAccountLoader } from "../common/SwitchingAccountLoader";
import TenantSelector from "../common/TenantSelector";
import UserProfileModal from "../common/UserProfileModal";
import { MobileMenu } from "./MobileMenu";
import { NavigationMenuItems } from "./NavigationMenuItems";
import "@repo/ui/tailwind.css";

type TenantInfo = components["schemas"]["TenantInfo"];

export type FederatedSideMenuProps = {
  currentSystem: "account-management" | "back-office"; // Add your self-contained system here
};

export default function FederatedSideMenu({ currentSystem }: Readonly<FederatedSideMenuProps>) {
  const _userInfo = useUserInfo();
  const [isProfileModalOpen, setIsProfileModalOpen] = useState(false);
  const [isSessionsModalOpen, setIsSessionsModalOpen] = useState(false);
  const [invitationDialogTenant, setInvitationDialogTenant] = useState<TenantInfo | null>(null);
  const [isSwitching, setIsSwitching] = useState(false);

  // Use the shared switch tenant hook
  const { switchTenant } = useSwitchTenant({
    onMutate: () => {
      setIsSwitching(true);
    },
    onSuccess: () => {
      // Stay on the same path, but default to loggedInPath if on root
      const targetPath = globalThis.location.pathname === "/" ? loggedInPath : globalThis.location.pathname;
      globalThis.location.href = targetPath;
    },
    onError: () => {
      setIsSwitching(false);
      setInvitationDialogTenant(null); // Close dialog on error
    }
  });

  const handleAcceptInvitation = () => {
    if (invitationDialogTenant) {
      switchTenant(invitationDialogTenant);
    }
  };

  return (
    <>
      <SideMenu
        sidebarToggleAriaLabel={t`Toggle sidebar`}
        mobileMenuAriaLabel={t`Open navigation menu`}
        topMenuContent={
          <MobileMenu
            currentSystem={currentSystem}
            onEditProfile={() => setIsProfileModalOpen(true)}
            onShowSessions={() => setIsSessionsModalOpen(true)}
            onShowInvitationDialog={setInvitationDialogTenant}
          />
        }
        logoContent={<TenantSelector onShowInvitationDialog={setInvitationDialogTenant} />}
      >
        <NavigationMenuItems currentSystem={currentSystem} />
      </SideMenu>
      <UserProfileModal isOpen={isProfileModalOpen} onOpenChange={setIsProfileModalOpen} />
      <SessionsModal isOpen={isSessionsModalOpen} onOpenChange={setIsSessionsModalOpen} />
      <AcceptInvitationDialog
        isOpen={!!invitationDialogTenant}
        onOpenChange={(open) => !open && setInvitationDialogTenant(null)}
        tenant={invitationDialogTenant}
        onAccept={handleAcceptInvitation}
        isLoading={isSwitching}
      />
      {isSwitching && <SwitchingAccountLoader />}
    </>
  );
}
