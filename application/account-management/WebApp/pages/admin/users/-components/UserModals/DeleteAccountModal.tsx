import { AlertDialog } from "@repo/ui/components/AlertDialog";
import { Button } from "@repo/ui/components/Button";
import { Modal } from "@repo/ui/components/Modal";
import { XIcon } from "lucide-react";
import { useState } from "react";

type DeleteAccountModalProps = {
  isOpen: boolean;
  onOpenChange: (isOpen: boolean) => void;
};

export function DeleteAccountModal({ isOpen, onOpenChange }: DeleteAccountModalProps) {
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

export default DeleteAccountModal;
