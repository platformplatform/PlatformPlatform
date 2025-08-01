import { useAuthSync } from "@repo/infrastructure/auth/useAuthSync";
import { Suspense, lazy } from "react";

// Lazy load the AuthSyncModal from account-management federated module
const AuthSyncModal = lazy(() => import("account-management/AuthSyncModal"));

/**
 * Simple wrapper for back-office that shows auth sync modal without tenant switching
 */
export function AuthSyncWrapper() {
  const { modalState, handlePrimaryAction } = useAuthSync();

  if (!modalState.isOpen) {
    return null;
  }

  return (
    <Suspense fallback={null}>
      <AuthSyncModal
        isOpen={modalState.isOpen}
        type={modalState.type}
        currentTenantName={modalState.currentTenantName}
        newTenantName={modalState.newTenantName}
        onPrimaryAction={handlePrimaryAction}
        onSecondaryAction={undefined}
      />
    </Suspense>
  );
}
