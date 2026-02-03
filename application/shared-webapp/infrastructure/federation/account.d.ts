// Type declarations for federated modules that are only available at runtime
declare module "account/AuthSyncModal" {
  import type { FC } from "react";

  interface AuthSyncModalProps {
    isOpen: boolean;
    type: "tenant-switch" | "logged-in" | "logged-out";
    newTenantName?: string;
    onPrimaryAction: () => void;
  }

  const AuthSyncModal: FC<AuthSyncModalProps>;
  export default AuthSyncModal;
}
