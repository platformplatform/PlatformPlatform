import { createContext, type ReactNode, useCallback } from "react";
import { useUnsavedChangesGuard } from "../hooks/useUnsavedChangesGuard";
import { Dialog, type DialogProps } from "./Dialog";
import { UnsavedChangesAlertDialog } from "./UnsavedChangesAlertDialog";

type DirtyDialogContextValue = {
  cancel: () => void;
};

export const DirtyDialogContext = createContext<DirtyDialogContextValue | null>(null);

export type DirtyDialogProps = Omit<DialogProps, "onOpenChange"> & {
  onOpenChange: (isOpen: boolean) => void;
  hasUnsavedChanges: boolean;
  unsavedChangesTitle?: string;
  unsavedChangesMessage?: ReactNode;
  leaveLabel?: string;
  stayLabel?: string;
  onCloseComplete?: () => void;
};

/**
 * A Dialog wrapper that warns users about unsaved changes before closing.
 * Encapsulates the useUnsavedChangesGuard hook and UnsavedChangesAlertDialog.
 *
 * Translation strings are passed as props to allow each SCS to provide
 * their own translations. English defaults are provided for convenience.
 */
export function DirtyDialog({
  open,
  onOpenChange,
  hasUnsavedChanges,
  unsavedChangesTitle = "Unsaved changes",
  unsavedChangesMessage = "You have unsaved changes. If you leave now, your changes will be lost.",
  leaveLabel = "Leave",
  stayLabel = "Stay",
  onCloseComplete,
  children,
  ...dialogProps
}: Readonly<DirtyDialogProps>) {
  const { isConfirmDialogOpen, confirmLeave, cancelLeave, guardedOnOpenChange } = useUnsavedChangesGuard({
    hasUnsavedChanges
  });

  const closeDialog = useCallback(() => {
    onOpenChange(false);
    onCloseComplete?.();
  }, [onOpenChange, onCloseComplete]);

  const handleDialogOpenChange = useCallback(
    (newOpen: boolean) => {
      if (newOpen) {
        onOpenChange(newOpen);
      } else {
        guardedOnOpenChange(newOpen, closeDialog);
      }
    },
    [guardedOnOpenChange, closeDialog, onOpenChange]
  );

  return (
    <>
      <DirtyDialogContext.Provider value={{ cancel: closeDialog }}>
        <Dialog open={open} onOpenChange={handleDialogOpenChange} {...dialogProps}>
          {children}
        </Dialog>
      </DirtyDialogContext.Provider>

      <UnsavedChangesAlertDialog
        isOpen={isConfirmDialogOpen}
        onConfirmLeave={confirmLeave}
        onCancel={cancelLeave}
        title={unsavedChangesTitle}
        actionLabel={leaveLabel}
        cancelLabel={stayLabel}
      >
        {unsavedChangesMessage}
      </UnsavedChangesAlertDialog>
    </>
  );
}
