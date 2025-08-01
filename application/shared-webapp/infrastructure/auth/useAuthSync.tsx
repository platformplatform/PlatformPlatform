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
      if (!userInfo) {
        return;
      }

      let shouldShowModal = false;
      let newModalState: ModalState | null = null;

      switch (message.type) {
        case "TENANT_SWITCHED":
          // Only show if we're authenticated and on a different tenant
          if (userInfo.isAuthenticated && userInfo.tenantId !== message.newTenantId) {
            shouldShowModal = true;
            newModalState = {
              isOpen: true,
              type: "tenant-switch",
              currentTenantName: userInfo.tenantName || "Current account",
              newTenantName: message.tenantName,
              newTenantId: message.newTenantId
            };
          } else if (userInfo.isAuthenticated && userInfo.tenantId === message.newTenantId) {
            // Same tenant - close any existing modal
            shouldShowModal = false;
            newModalState = { isOpen: false, type: "tenant-switch" };
          }
          break;

        case "USER_LOGGED_IN":
          if (!userInfo.isAuthenticated) {
            // We were logged out, now someone logged in
            shouldShowModal = true;
            newModalState = {
              isOpen: true,
              type: "logged-in"
            };
          } else if (message.email && userInfo.email !== message.email) {
            // Different user logged in
            shouldShowModal = true;
            newModalState = {
              isOpen: true,
              type: "logged-in"
            };
          } else if (message.tenantId && userInfo.tenantId !== message.tenantId) {
            // Same user, different tenant - show tenant switch
            shouldShowModal = true;
            newModalState = {
              isOpen: true,
              type: "tenant-switch",
              currentTenantName: userInfo.tenantName || "Current account",
              newTenantName: undefined, // We don't have tenant name in login message
              newTenantId: message.tenantId
            };
          } else {
            // Same user, same tenant - close any existing modal
            shouldShowModal = false;
            newModalState = { isOpen: false, type: "logged-in" };
          }
          break;

        case "USER_LOGGED_OUT":
          if (userInfo.isAuthenticated) {
            shouldShowModal = true;
            newModalState = {
              isOpen: true,
              type: "logged-out"
            };
          }
          break;

        default:
          break;
      }

      // Update modal state if we have a new state
      if (newModalState) {
        setModalState(newModalState);
      }

      // Update the pending sync state
      setHasPendingAuthSync(shouldShowModal);
    },
    [userInfo]
  );

  // Handle visibility changes - process the last pending message when tab becomes visible
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
        // Store only the last message for when tab becomes visible
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
