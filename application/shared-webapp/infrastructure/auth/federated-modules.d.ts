// Type declarations for federated modules that are only available at runtime
declare module "account-management/AuthSyncModal" {
  import type { FC } from "react";

  interface AuthSyncModalProps {
    isOpen: boolean;
    type: "tenant-switch" | "logged-in" | "logged-out";
    currentTenantName?: string;
    newTenantName?: string;
    onPrimaryAction: () => void;
    onSecondaryAction?: () => void;
  }

  const AuthSyncModal: FC<AuthSyncModalProps>;
  export default AuthSyncModal;
}
