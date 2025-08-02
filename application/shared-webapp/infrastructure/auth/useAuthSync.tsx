import { useCallback, useEffect, useState } from "react";
import type { AuthSyncMessage, TenantSwitchedMessage, UserLoggedInMessage } from "./AuthSyncService";
import { authSyncService, setHasPendingAuthSync } from "./AuthSyncService";
import { useUserInfo } from "./hooks";
import { getCurrentSanitizedUrl } from "./urlSanitizer";

export type AuthSyncModalType = "tenant-switch" | "logged-in" | "logged-out";

export interface ModalState {
  isOpen: boolean;
  type: AuthSyncModalType;
  newTenantName?: string;
  newTenantId?: string;
}

interface ProcessResult {
  shouldShowModal: boolean;
  newModalState: ModalState | null;
}

function processTenantSwitch(
  message: TenantSwitchedMessage,
  userInfo: NonNullable<ReturnType<typeof useUserInfo>>
): ProcessResult {
  if (!userInfo.isAuthenticated) {
    return { shouldShowModal: false, newModalState: null };
  }

  if (userInfo.tenantId !== message.newTenantId) {
    return {
      shouldShowModal: true,
      newModalState: {
        isOpen: true,
        type: "tenant-switch",
        newTenantName: message.tenantName,
        newTenantId: message.newTenantId
      }
    };
  }

  // Same tenant - close any existing modal
  return {
    shouldShowModal: false,
    newModalState: { isOpen: false, type: "tenant-switch" }
  };
}

function processUserLogin(
  message: UserLoggedInMessage,
  userInfo: NonNullable<ReturnType<typeof useUserInfo>>
): ProcessResult {
  if (!userInfo.isAuthenticated) {
    // We were logged out, now someone logged in
    return {
      shouldShowModal: true,
      newModalState: { isOpen: true, type: "logged-in" }
    };
  }

  if (message.email && userInfo.email !== message.email) {
    // Different user logged in
    return {
      shouldShowModal: true,
      newModalState: { isOpen: true, type: "logged-in" }
    };
  }

  if (message.tenantId && userInfo.tenantId !== message.tenantId) {
    // Same user, different tenant - show tenant switch
    return {
      shouldShowModal: true,
      newModalState: {
        isOpen: true,
        type: "tenant-switch",
        newTenantName: undefined,
        newTenantId: message.tenantId
      }
    };
  }

  // Same user, same tenant - close any existing modal
  return {
    shouldShowModal: false,
    newModalState: { isOpen: false, type: "logged-in" }
  };
}

function processUserLogout(userInfo: NonNullable<ReturnType<typeof useUserInfo>>): ProcessResult {
  if (userInfo.isAuthenticated) {
    return {
      shouldShowModal: true,
      newModalState: { isOpen: true, type: "logged-out" }
    };
  }
  return { shouldShowModal: false, newModalState: null };
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

      let result: ProcessResult;

      switch (message.type) {
        case "TENANT_SWITCHED":
          result = processTenantSwitch(message, userInfo);
          break;

        case "USER_LOGGED_IN":
          result = processUserLogin(message, userInfo);
          break;

        case "USER_LOGGED_OUT":
          result = processUserLogout(userInfo);
          break;

        default:
          result = { shouldShowModal: false, newModalState: null };
          break;
      }

      // Update modal state if we have a new state
      if (result.newModalState) {
        setModalState(result.newModalState);
      }

      // Update the pending sync state
      setHasPendingAuthSync(result.shouldShowModal);
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
