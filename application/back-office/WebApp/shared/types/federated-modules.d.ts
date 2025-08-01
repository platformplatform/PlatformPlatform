declare module "account-management/AuthSyncModal" {
  import type { AuthSyncModalType } from "@repo/infrastructure/auth/useAuthSync";

  export interface AuthSyncModalProps {
    isOpen: boolean;
    type: AuthSyncModalType;
    currentTenantName?: string;
    newTenantName?: string;
    onPrimaryAction: () => void;
    onSecondaryAction?: () => void;
  }

  const AuthSyncModal: React.FC<AuthSyncModalProps>;
  export default AuthSyncModal;
}
