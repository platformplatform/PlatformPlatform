import type { components } from "@/shared/lib/api/api.generated";
import { api } from "@/shared/lib/api/client";
import { t } from "@lingui/core/macro";
import { useUserInfo } from "@repo/infrastructure/auth/hooks";
import { SideMenu } from "@repo/ui/components/SideMenu";
import { useState } from "react";
import { AcceptInvitationDialog } from "../common/AcceptInvitationDialog";
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
  const userInfo = useUserInfo();
  const [isProfileModalOpen, setIsProfileModalOpen] = useState(false);
  const [invitationDialogTenant, setInvitationDialogTenant] = useState<TenantInfo | null>(null);

  const switchTenantMutation = api.useMutation("post", "/api/account-management/authentication/switch-tenant", {
    onSuccess: () => {
      // Redirect to logged-in path after successful switch
      window.location.href = "/";
    }
  });

  const handleAcceptInvitation = () => {
    if (invitationDialogTenant && userInfo) {
      localStorage.setItem(`preferred-tenant-${userInfo.email}`, invitationDialogTenant.tenantId);
      switchTenantMutation.mutate({ body: { tenantId: invitationDialogTenant.tenantId } });
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
            onShowInvitationDialog={setInvitationDialogTenant}
          />
        }
        logoContent={<TenantSelector onShowInvitationDialog={setInvitationDialogTenant} />}
      >
        <NavigationMenuItems currentSystem={currentSystem} />
      </SideMenu>
      <UserProfileModal isOpen={isProfileModalOpen} onOpenChange={setIsProfileModalOpen} />
      <AcceptInvitationDialog
        isOpen={!!invitationDialogTenant}
        onOpenChange={(open) => !open && setInvitationDialogTenant(null)}
        tenant={invitationDialogTenant}
        onAccept={handleAcceptInvitation}
        isLoading={switchTenantMutation.isPending}
      />
    </>
  );
}
