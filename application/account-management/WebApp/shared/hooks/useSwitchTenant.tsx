import { t } from "@lingui/core/macro";
import { authSyncService, type TenantSwitchedMessage } from "@repo/infrastructure/auth/AuthSyncService";
import { useUserInfo } from "@repo/infrastructure/auth/hooks";
import type { components } from "@/shared/lib/api/api.generated";
import { api } from "@/shared/lib/api/client";

type TenantInfo = components["schemas"]["TenantInfo"];

interface UseSwitchTenantOptions {
  onMutate?: () => void;
  onSuccess?: () => void;
  onError?: (error: unknown) => void;
}

export function useSwitchTenant(options?: UseSwitchTenantOptions) {
  const userInfo = useUserInfo();

  const switchTenantMutation = api.useMutation("post", "/api/account-management/authentication/switch-tenant", {
    onMutate: options?.onMutate,
    onSuccess: (_, { body: { tenantId } }) => {
      // Broadcast the tenant switch to other tabs AFTER successful switch
      if (userInfo?.tenantId && userInfo?.id && tenantId !== userInfo.tenantId) {
        const tenantName = localStorage.getItem(`tenant-name-${tenantId}`) || t`Unnamed account`;
        const message: Omit<TenantSwitchedMessage, "timestamp"> = {
          type: "TENANT_SWITCHED",
          newTenantId: tenantId,
          previousTenantId: userInfo.tenantId,
          tenantName,
          userId: userInfo.id
        };
        authSyncService.broadcast(message);
      }
      options?.onSuccess?.();
    },
    onError: (error) => {
      options?.onError?.(error);
      // Ensure unhandled rejection triggers global error handler
      setTimeout(() => Promise.reject(error), 0);
    }
  });

  const switchTenant = ({ tenantId, tenantName }: TenantInfo) => {
    // Store preferences
    localStorage.setItem("preferred-tenant", tenantId);
    if (tenantName) {
      localStorage.setItem(`tenant-name-${tenantId}`, tenantName);
    }

    switchTenantMutation.mutate({ body: { tenantId } });
  };

  return {
    switchTenant,
    ...switchTenantMutation
  };
}
