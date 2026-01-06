import type { ReactNode } from "react";
import { AlertDialog } from "./AlertDialog";
import { Modal } from "./Modal";

type UnsavedChangesAlertDialogProps = {
  isOpen: boolean;
  onConfirmLeave: () => void;
  onCancel: () => void;
  title: string;
  actionLabel: string;
  cancelLabel: string;
  children: ReactNode;
};

export function UnsavedChangesAlertDialog({
  isOpen,
  onConfirmLeave,
  onCancel,
  title,
  actionLabel,
  cancelLabel,
  children
}: Readonly<UnsavedChangesAlertDialogProps>) {
  return (
    <Modal isOpen={isOpen} onOpenChange={(open) => !open && onCancel()} zIndex="high">
      <AlertDialog
        title={title}
        variant="destructive"
        actionLabel={actionLabel}
        cancelLabel={cancelLabel}
        onAction={onConfirmLeave}
      >
        {children}
      </AlertDialog>
    </Modal>
  );
}
