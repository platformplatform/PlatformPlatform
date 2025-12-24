import type { ReactNode } from "react";
import {
  AlertDialog,
  AlertDialogClose,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle
} from "./AlertDialog";
import { Button } from "./Button";

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
    <AlertDialog open={isOpen} onOpenChange={(open) => !open && onCancel()}>
      <AlertDialogContent zIndex="high">
        <AlertDialogHeader>
          <AlertDialogTitle>{title}</AlertDialogTitle>
          <AlertDialogDescription>{children}</AlertDialogDescription>
        </AlertDialogHeader>
        <AlertDialogFooter>
          <AlertDialogClose render={<Button variant="secondary" />}>{cancelLabel}</AlertDialogClose>
          <Button variant="destructive" onClick={onConfirmLeave}>
            {actionLabel}
          </Button>
        </AlertDialogFooter>
      </AlertDialogContent>
    </AlertDialog>
  );
}
