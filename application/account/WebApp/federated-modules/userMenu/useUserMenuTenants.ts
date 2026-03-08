import type { useUserInfo } from "@repo/infrastructure/auth/hooks";

import { t } from "@lingui/core/macro";
import { trackInteraction } from "@repo/infrastructure/applicationInsights/ApplicationInsightsProvider";
import { authSyncService, type TenantSwitchedMessage } from "@repo/infrastructure/auth/AuthSyncService";
import { loggedInPath } from "@repo/infrastructure/auth/constants";
import { useEffect, useState } from "react";

import { fetchTenants, sortTenants, switchTenantApi, type TenantInfo } from "../common/tenantUtils";

export function useUserMenuTenants(isMenuOpen: boolean, userInfo: ReturnType<typeof useUserInfo>) {
  const [tenants, setTenants] = useState<TenantInfo[]>([]);
  const [isLoadingTenants, setIsLoadingTenants] = useState(false);
  const [isSwitching, setIsSwitching] = useState(false);

  useEffect(() => {
    if (isMenuOpen && userInfo?.isAuthenticated) {
      setIsLoadingTenants(true);
      fetchTenants()
        .then((response) => {
          setTenants(response.tenants || []);
        })
        .catch(() => {
          setTenants([]);
        })
        .finally(() => {
          setIsLoadingTenants(false);
        });
    }
  }, [isMenuOpen, userInfo?.isAuthenticated]);

  useEffect(() => {
    const handleTenantUpdated = () => {
      if (userInfo?.isAuthenticated) {
        fetchTenants()
          .then((response) => {
            setTenants(response.tenants || []);
          })
          .catch(() => {});
      }
    };

    window.addEventListener("tenant-updated", handleTenantUpdated);
    return () => window.removeEventListener("tenant-updated", handleTenantUpdated);
  }, [userInfo?.isAuthenticated]);

  const currentTenantId = userInfo?.tenantId;
  const acceptedTenants = tenants.filter((t) => !t.isNew);
  const sortedTenants = sortTenants(acceptedTenants);
  const currentTenant = tenants.find((t) => t.tenantId === currentTenantId);

  const handleTenantSwitch = async (tenant: TenantInfo) => {
    if (tenant.tenantId === currentTenantId) {
      return;
    }

    trackInteraction("Switch account", "interaction");
    setIsSwitching(true);
    try {
      localStorage.setItem("preferred-tenant", tenant.tenantId);
      if (tenant.tenantName) {
        localStorage.setItem(`tenant-name-${tenant.tenantId}`, tenant.tenantName);
      }

      await switchTenantApi(tenant.tenantId);

      if (userInfo?.tenantId && userInfo?.id) {
        const message: Omit<TenantSwitchedMessage, "timestamp"> = {
          type: "TENANT_SWITCHED",
          newTenantId: tenant.tenantId,
          previousTenantId: userInfo.tenantId,
          tenantName: tenant.tenantName || t`Unnamed account`,
          userId: userInfo.id
        };
        authSyncService.broadcast(message);
      }

      const targetPath = window.location.pathname === "/" ? loggedInPath : window.location.pathname;
      window.location.href = targetPath;
    } catch {
      setIsSwitching(false);
    }
  };

  return {
    tenants,
    sortedTenants,
    currentTenant,
    currentTenantId,
    isLoadingTenants,
    isSwitching,
    handleTenantSwitch
  };
}
