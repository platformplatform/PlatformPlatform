import { useBlocker } from "@tanstack/react-router";
import { useCallback, useEffect, useRef, useState } from "react";
import { registerNavigationGuard } from "./federatedNavigationGuard";

type UseUnsavedChangesGuardOptions = {
  hasUnsavedChanges: boolean;
};

type UseUnsavedChangesGuardResult = {
  isConfirmDialogOpen: boolean;
  confirmLeave: () => void;
  cancelLeave: () => void;
  guardedOnOpenChange: (open: boolean, originalHandler: (open: boolean) => void) => void;
};

export function useUnsavedChangesGuard({
  hasUnsavedChanges
}: UseUnsavedChangesGuardOptions): UseUnsavedChangesGuardResult {
  const [isConfirmDialogOpen, setIsConfirmDialogOpen] = useState(false);
  const [pendingCloseHandler, setPendingCloseHandler] = useState<(() => void) | null>(null);

  // Use a ref so the shouldBlockFn always reads the latest value without stale closures
  const hasUnsavedChangesRef = useRef(hasUnsavedChanges);
  hasUnsavedChangesRef.current = hasUnsavedChanges;

  useEffect(() => {
    return registerNavigationGuard(() => hasUnsavedChanges);
  }, [hasUnsavedChanges]);

  // Handle browser back/forward navigation, in-app navigation, and tab/window close
  const { proceed, reset, status } = useBlocker({
    shouldBlockFn: () => hasUnsavedChangesRef.current,
    withResolver: true,
    enableBeforeUnload: true
  });

  // Show our custom dialog when navigation is blocked
  useEffect(() => {
    if (status === "blocked") {
      setIsConfirmDialogOpen(true);
    }
  }, [status]);

  const confirmLeave = useCallback(() => {
    setIsConfirmDialogOpen(false);

    // If we have a pending close handler (dialog close), call it
    if (pendingCloseHandler) {
      pendingCloseHandler();
      setPendingCloseHandler(null);
      return;
    }

    // If navigation was blocked, proceed with navigation
    if (status === "blocked") {
      proceed();
    }
  }, [pendingCloseHandler, status, proceed]);

  const cancelLeave = useCallback(() => {
    setIsConfirmDialogOpen(false);
    setPendingCloseHandler(null);

    // If navigation was blocked, reset the blocker
    if (status === "blocked") {
      reset();
    }
  }, [status, reset]);

  // Wrapper for dialog onOpenChange that intercepts close attempts
  const guardedOnOpenChange = useCallback(
    (open: boolean, originalHandler: (open: boolean) => void) => {
      // If trying to close and we have unsaved changes, show confirmation
      if (!open && hasUnsavedChanges) {
        setPendingCloseHandler(() => () => originalHandler(false));
        setIsConfirmDialogOpen(true);
        return;
      }

      // Otherwise, proceed normally
      originalHandler(open);
    },
    [hasUnsavedChanges]
  );

  return {
    isConfirmDialogOpen,
    confirmLeave,
    cancelLeave,
    guardedOnOpenChange
  };
}
