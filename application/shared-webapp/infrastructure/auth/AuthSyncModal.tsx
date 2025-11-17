import { useRouterState } from "@tanstack/react-router";
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
 *
 * Routes can opt out by adding: beforeLoad: () => ({ disableAuthSync: true })
 * This is useful for public pages that should remain standalone.
 */
export function AuthSyncModal({ modalComponent: ModalComponent }: AuthSyncRendererProps) {
  const routerState = useRouterState();
  const { modalState, handlePrimaryAction } = useAuthSync();

  // Check if current route opts out of auth sync modal
  const currentRoute = routerState.matches.at(-1);
  const disableAuthSync =
    (currentRoute?.context as { disableAuthSync?: boolean } | undefined)?.disableAuthSync ?? false;

  if (disableAuthSync || !modalState.isOpen) {
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
