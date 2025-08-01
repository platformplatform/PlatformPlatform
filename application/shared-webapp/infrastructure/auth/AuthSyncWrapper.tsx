import { Suspense, lazy } from "react";
import type { ReactNode } from "react";
import { AuthSyncProvider } from "./AuthSyncProvider";

interface AuthSyncWrapperProps {
  children: ReactNode;
}

/**
 * Wrapper component that provides authentication synchronization across browser tabs.
 *
 * This wrapper lazy loads the AuthSyncModal from the account-management federated module.
 * For the host system (account-management), use AuthSyncWrapperHost instead.
 */
export function AuthSyncWrapper({ children }: AuthSyncWrapperProps) {
  // Lazy load from account-management federated module
  const AuthSyncModal = lazy(() => import("account-management/AuthSyncModal"));

  return (
    <AuthSyncProvider modalComponent={AuthSyncModal}>
      <Suspense fallback={null}>{children}</Suspense>
    </AuthSyncProvider>
  );
}
