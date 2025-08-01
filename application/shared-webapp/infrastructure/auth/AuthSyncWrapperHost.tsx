import type { ComponentType, ReactNode } from "react";
import { AuthSyncModalRenderer } from "./AuthSyncModalRenderer";
import { type AuthSyncModalProps, AuthSyncProvider } from "./AuthSyncProvider";

interface AuthSyncWrapperHostProps {
  children: ReactNode;
  modalComponent: ComponentType<AuthSyncModalProps>;
}

/**
 * Wrapper component for the host system (account-management) that provides the AuthSyncModal.
 * This version doesn't have any lazy imports to avoid circular dependencies.
 */
export function AuthSyncWrapperHost({ children, modalComponent }: AuthSyncWrapperHostProps) {
  return (
    <AuthSyncProvider modalComponent={modalComponent}>
      <AuthSyncModalRenderer modalComponent={modalComponent}>{children}</AuthSyncModalRenderer>
    </AuthSyncProvider>
  );
}
