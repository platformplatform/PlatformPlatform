import { AlertDialog } from "@repo/ui/components/AlertDialog";
import { Modal } from "@repo/ui/components/Modal";

type DeleteAccountConfirmationProps = {
  isOpen: boolean;
  onOpenChange: (isOpen: boolean) => void;
};

export function DeleteAccountConfirmation({ isOpen, onOpenChange }: Readonly<DeleteAccountConfirmationProps>) {
  return (
    <Modal isOpen={isOpen} onOpenChange={onOpenChange} isDismissable>
      <AlertDialog
        variant="destructive"
        actionLabel="Delete Account"
        title="Delete Account"
        onAction={() => onOpenChange(false)}
      >
        You’re about to permanently delete the account and all data...
        <br />
        environment through PlatformPlatform.
        <br />
        <br />
        Are you sure you want to delete?
        <br />
        This action is permanent and irreversible.
      </AlertDialog>
    </Modal>
  );
}

export default DeleteAccountConfirmation;
