import type { ComponentType, ReactNode } from "react";
import { useAuthSync } from "./useAuthSync";

// Type for the AuthSyncModal component
export interface AuthSyncModalProps {
  isOpen: boolean;
  type: "tenant-switch" | "logged-in" | "logged-out";
  currentTenantName?: string;
  newTenantName?: string;
  onPrimaryAction: () => void;
  onSecondaryAction?: () => void;
}

interface AuthSyncProviderProps {
  children: ReactNode;
  modalComponent: ComponentType<AuthSyncModalProps>;
}

/**
 * Provider component that handles authentication synchronization across tabs.
 * The modal component is injected to avoid circular dependencies.
 */
export function AuthSyncProvider({ children, modalComponent: ModalComponent }: AuthSyncProviderProps) {
  const { modalState, handlePrimaryAction, handleSecondaryAction } = useAuthSync();

  return (
    <>
      {children}
      {modalState.isOpen && (
        <ModalComponent
          isOpen={modalState.isOpen}
          type={modalState.type}
          currentTenantName={modalState.currentTenantName}
          newTenantName={modalState.newTenantName}
          onPrimaryAction={handlePrimaryAction}
          onSecondaryAction={modalState.type === "tenant-switch" ? handleSecondaryAction : undefined}
        />
      )}
    </>
  );
}
