import { useCallback, useEffect, useState } from "react";
import type { AuthSyncMessage } from "./AuthSyncService";
import { authSyncService } from "./AuthSyncService";
import { setHasPendingAuthSync } from "./authSyncState";
import { useUserInfo } from "./hooks";
import { getCurrentSanitizedUrl } from "./urlSanitizer";

export type AuthSyncModalType = "tenant-switch" | "logged-in" | "logged-out";

export interface ModalState {
  isOpen: boolean;
  type: AuthSyncModalType;
  currentTenantName?: string;
  newTenantName?: string;
  newTenantId?: string;
}

/**
 * Hook to synchronize authentication state across browser tabs
 *
 * Listens for authentication events from other tabs and returns
 * modal state when synchronization is needed
 */
export function useAuthSync() {
  const userInfo = useUserInfo();
  const [modalState, setModalState] = useState<ModalState>({
    isOpen: false,
    type: "tenant-switch"
  });
  const [pendingMessage, setPendingMessage] = useState<AuthSyncMessage | null>(null);

  const processSyncMessage = useCallback(
    (message: AuthSyncMessage) => {
      let shouldShowModal = false;

      switch (message.type) {
        case "TENANT_SWITCHED":
          // Check if current tenant differs from the new tenant
          if (userInfo?.isAuthenticated && userInfo.tenantId !== message.newTenantId) {
            shouldShowModal = true;
            setModalState({
              isOpen: true,
              type: "tenant-switch",
              currentTenantName: userInfo.tenantName || "Current Account",
              newTenantName: message.tenantName,
              newTenantId: message.newTenantId
            });
          }
          break;

        case "USER_LOGGED_IN":
          // Show modal if currently logged out
          if (!userInfo?.isAuthenticated) {
            shouldShowModal = true;
            setModalState({
              isOpen: true,
              type: "logged-in"
            });
          }
          break;

        case "USER_LOGGED_OUT":
          // Show modal if currently logged in
          if (userInfo?.isAuthenticated) {
            shouldShowModal = true;
            setModalState({
              isOpen: true,
              type: "logged-out"
            });
          }
          break;

        default:
          // Exhaustive check - should never reach here
          break;
      }

      // Update the pending sync state
      setHasPendingAuthSync(shouldShowModal);
    },
    [userInfo]
  );

  // Handle visibility changes - show modal when tab becomes visible
  useEffect(() => {
    const handleVisibilityChange = () => {
      if (!document.hidden && pendingMessage) {
        processSyncMessage(pendingMessage);
        setPendingMessage(null);
      }
    };

    document.addEventListener("visibilitychange", handleVisibilityChange);
    return () => document.removeEventListener("visibilitychange", handleVisibilityChange);
  }, [pendingMessage, processSyncMessage]);

  useEffect(() => {
    const unsubscribe = authSyncService.subscribe((message: AuthSyncMessage) => {
      // If tab is visible, process immediately
      if (!document.hidden) {
        processSyncMessage(message);
      } else {
        // Queue message for when tab becomes visible
        setPendingMessage(message);
      }
    });

    return unsubscribe;
  }, [processSyncMessage]);

  const handlePrimaryAction = () => {
    // Clear pending state before reload
    setHasPendingAuthSync(false);

    // Reload to the sanitized URL
    const sanitizedUrl = getCurrentSanitizedUrl();
    window.location.href = sanitizedUrl;
  };

  return {
    modalState,
    handlePrimaryAction
  };
}
