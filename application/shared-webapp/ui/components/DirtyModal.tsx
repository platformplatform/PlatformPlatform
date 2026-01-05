import type { ReactNode } from "react";
import { useCallback } from "react";
import { useUnsavedChangesGuard } from "../hooks/useUnsavedChangesGuard";
import { Modal, type ModalProps } from "./Modal";
import { UnsavedChangesAlertDialog } from "./UnsavedChangesAlertDialog";

export type DirtyModalProps = Omit<ModalProps, "onOpenChange"> & {
  onOpenChange: (isOpen: boolean) => void;
  hasUnsavedChanges: boolean;
  unsavedChangesTitle?: string;
  unsavedChangesMessage?: ReactNode;
  leaveLabel?: string;
  stayLabel?: string;
  onCloseComplete?: () => void;
};

/**
 * A Modal wrapper that warns users about unsaved changes before closing.
 * Encapsulates the useUnsavedChangesGuard hook and UnsavedChangesAlertDialog.
 *
 * Translation strings are passed as props to allow each SCS to provide
 * their own translations. English defaults are provided for convenience.
 */
export function DirtyModal({
  isOpen,
  onOpenChange,
  hasUnsavedChanges,
  unsavedChangesTitle = "Unsaved changes",
  unsavedChangesMessage = "You have unsaved changes. If you leave now, your changes will be lost.",
  leaveLabel = "Leave",
  stayLabel = "Stay",
  onCloseComplete,
  children,
  ...modalProps
}: Readonly<DirtyModalProps>) {
  const { isConfirmDialogOpen, confirmLeave, cancelLeave, guardedOnOpenChange } = useUnsavedChangesGuard({
    hasUnsavedChanges
  });

  const closeDialog = useCallback(() => {
    onOpenChange(false);
    onCloseComplete?.();
  }, [onOpenChange, onCloseComplete]);

  const handleModalOpenChange = useCallback(
    (open: boolean) => {
      if (open) {
        onOpenChange(open);
      } else {
        guardedOnOpenChange(open, closeDialog);
      }
    },
    [guardedOnOpenChange, closeDialog, onOpenChange]
  );

  return (
    <>
      <Modal isOpen={isOpen} onOpenChange={handleModalOpenChange} {...modalProps}>
        {children}
      </Modal>

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
