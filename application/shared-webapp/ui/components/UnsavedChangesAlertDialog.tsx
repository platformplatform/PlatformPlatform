import { AlertCircleIcon } from "lucide-react";
import type { ReactNode } from "react";
import { Dialog } from "react-aria-components";
import { Button } from "./Button";
import { Heading } from "./Heading";
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
      <Dialog role="alertdialog" className="relative sm:w-dialog-md" aria-label={title}>
        {({ close }) => (
          <>
            <Heading slot="title">{title}</Heading>
            <div className="absolute top-6 right-6 h-6 w-6 stroke-2 text-destructive">
              <AlertCircleIcon aria-hidden={true} />
            </div>
            <div>{children}</div>
            <fieldset className="flex justify-end gap-2 pt-10">
              <Button variant="secondary" onClick={close}>
                {cancelLabel}
              </Button>
              <Button
                variant="destructive"
                autoFocus={true}
                onClick={() => {
                  onConfirmLeave();
                  close();
                }}
              >
                {actionLabel}
              </Button>
            </fieldset>
          </>
        )}
      </Dialog>
    </Modal>
  );
}
