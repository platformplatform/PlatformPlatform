import type { ReactNode } from "react";
import { AlertDialog, AlertDialogRoot } from "./AlertDialog";

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
    <AlertDialogRoot open={isOpen} onOpenChange={(open) => !open && onCancel()}>
      <AlertDialog
        title={title}
        variant="destructive"
        actionLabel={actionLabel}
        cancelLabel={cancelLabel}
        onAction={onConfirmLeave}
      >
        {children}
      </AlertDialog>
    </AlertDialogRoot>
  );
}
