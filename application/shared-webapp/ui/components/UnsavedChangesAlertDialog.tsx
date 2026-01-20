import { AlertCircleIcon } from "lucide-react";
import type { ReactNode } from "react";
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle
} from "./AlertDialog";

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
      <AlertDialogContent>
        <div className="absolute top-6 right-6 size-6 stroke-2 text-destructive">
          <AlertCircleIcon aria-hidden={true} />
        </div>
        <AlertDialogHeader>
          <AlertDialogTitle>{title}</AlertDialogTitle>
          <AlertDialogDescription>{children}</AlertDialogDescription>
        </AlertDialogHeader>
        <AlertDialogFooter>
          <AlertDialogCancel variant="secondary">{cancelLabel}</AlertDialogCancel>
          <AlertDialogAction variant="destructive" onClick={onConfirmLeave}>
            {actionLabel}
          </AlertDialogAction>
        </AlertDialogFooter>
      </AlertDialogContent>
    </AlertDialog>
  );
}
