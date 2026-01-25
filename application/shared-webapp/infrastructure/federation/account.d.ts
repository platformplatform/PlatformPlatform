// Type declarations for federated modules that are only available at runtime
declare module "account/FederatedSideMenu" {
  import type { FC } from "react";
  interface FederatedSideMenuProps {
    currentSystem: string;
  }
  const FederatedSideMenu: FC<FederatedSideMenuProps>;
  export default FederatedSideMenu;
}

declare module "account/FederatedTopMenu" {
  import type { FC, ReactNode } from "react";
  interface FederatedTopMenuProps {
    children?: ReactNode;
  }
  const FederatedTopMenu: FC<FederatedTopMenuProps>;
  export default FederatedTopMenu;
}

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
