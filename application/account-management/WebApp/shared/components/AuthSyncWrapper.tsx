import AuthSyncModal from "@/federated-modules/common/AuthSyncModal";
import { api } from "@/shared/lib/api/client";
import { type TenantSwitchedMessage, authSyncService } from "@repo/infrastructure/auth/AuthSyncService";
import { useUserInfo } from "@repo/infrastructure/auth/hooks";
import { useAuthSync } from "@repo/infrastructure/auth/useAuthSync";

/**
 * Wrapper component that provides the AuthSyncModal with tenant switching functionality
 */
export function AuthSyncWrapper() {
  const userInfo = useUserInfo();
  const { modalState, handlePrimaryAction } = useAuthSync();
  const switchTenantMutation = api.useMutation("post", "/api/account-management/authentication/switch-tenant");

  const handleSecondaryAction = () => {
    if (modalState.type === "tenant-switch" && userInfo?.tenantId) {
      // Switch back to current tenant
      switchTenantMutation.mutate(
        { body: { tenantId: userInfo.tenantId } },
        {
          onSuccess: () => {
            // Broadcast the switch back
            const message: Omit<TenantSwitchedMessage, "timestamp"> = {
              type: "TENANT_SWITCHED",
              newTenantId: userInfo.tenantId || "",
              previousTenantId: modalState.newTenantId || "",
              tenantName: modalState.currentTenantName || ""
            };
            authSyncService.broadcast(message);

            // Reload after a brief delay
            setTimeout(() => {
              window.location.href = "/";
            }, 250);
          }
        }
      );
    }
  };

  if (!modalState.isOpen) {
    return null;
  }

  return (
    <AuthSyncModal
      isOpen={modalState.isOpen}
      type={modalState.type}
      currentTenantName={modalState.currentTenantName}
      newTenantName={modalState.newTenantName}
      onPrimaryAction={handlePrimaryAction}
      onSecondaryAction={modalState.type === "tenant-switch" ? handleSecondaryAction : undefined}
    />
  );
}
