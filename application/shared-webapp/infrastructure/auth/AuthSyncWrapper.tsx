import { Suspense } from "react";
import type { ComponentType, ReactNode } from "react";
import { type AuthSyncModalProps, AuthSyncProvider } from "./AuthSyncProvider";

interface AuthSyncWrapperProps {
  children: ReactNode;
  modalComponent: ComponentType<AuthSyncModalProps>;
}

/**
 * Wrapper component that provides authentication synchronization across browser tabs.
 *
 * The modalComponent must be provided by the consuming application.
 */
export function AuthSyncWrapper({ children, modalComponent }: AuthSyncWrapperProps) {
  return (
    <AuthSyncProvider modalComponent={modalComponent}>
      <Suspense fallback={null}>{children}</Suspense>
    </AuthSyncProvider>
  );
}
