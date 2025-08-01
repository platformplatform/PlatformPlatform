import type { ComponentType, ReactNode } from "react";
import type { AuthSyncModalProps } from "./AuthSyncProvider";
import { useAuthSync } from "./useAuthSync";

interface AuthSyncModalRendererProps {
  children: ReactNode;
  modalComponent: ComponentType<AuthSyncModalProps>;
}

/**
 * Component that renders the AuthSyncModal when needed.
 * This is used by the host system (account-management) to render the modal.
 */
export function AuthSyncModalRenderer({ children, modalComponent: Modal }: AuthSyncModalRendererProps) {
  const { modalState, handlePrimaryAction } = useAuthSync();

  return (
    <>
      {children}
      {modalState.isOpen && (
        <Modal
          isOpen={modalState.isOpen}
          type={modalState.type}
          currentTenantName={modalState.currentTenantName}
          newTenantName={modalState.newTenantName}
          onPrimaryAction={handlePrimaryAction}
        />
      )}
    </>
  );
}
