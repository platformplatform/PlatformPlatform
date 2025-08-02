import type { ComponentType } from "react";
import { useAuthSync } from "./useAuthSync";

// Type for the AuthSyncModal component
export interface AuthSyncModalProps {
  isOpen: boolean;
  type: "tenant-switch" | "logged-in" | "logged-out";
  newTenantName?: string;
  onPrimaryAction: () => void;
}

interface AuthSyncRendererProps {
  modalComponent: ComponentType<AuthSyncModalProps>;
}

/**
 * Component that renders the authentication sync modal when needed.
 * Place this as a sibling to your main app content, not as a wrapper.
 * The modalComponent must be provided by the consuming application to avoid circular dependencies.
 */
export function AuthSyncModal({ modalComponent: ModalComponent }: AuthSyncRendererProps) {
  const { modalState, handlePrimaryAction } = useAuthSync();

  if (!modalState.isOpen) {
    return null;
  }

  return (
    <ModalComponent
      isOpen={modalState.isOpen}
      type={modalState.type}
      newTenantName={modalState.newTenantName}
      onPrimaryAction={handlePrimaryAction}
    />
  );
}
