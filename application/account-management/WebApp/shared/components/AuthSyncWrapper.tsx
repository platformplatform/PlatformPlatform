import AuthSyncModal from "@/federated-modules/common/AuthSyncModal";
import { useAuthSync } from "@repo/infrastructure/auth/useAuthSync";

/**
 * Wrapper component that provides the AuthSyncModal
 */
export function AuthSyncWrapper() {
  const { modalState, handlePrimaryAction } = useAuthSync();

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
    />
  );
}
