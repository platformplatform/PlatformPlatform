import { AlertDialog } from "@repo/ui/components/AlertDialog";
import { Modal } from "@repo/ui/components/Modal";
import { t, Trans } from "@lingui/macro";

type DeleteAccountConfirmationProps = {
  isOpen: boolean;
  onOpenChange: (isOpen: boolean) => void;
};

export default function DeleteAccountConfirmation({ isOpen, onOpenChange }: Readonly<DeleteAccountConfirmationProps>) {
  return (
    <Modal isOpen={isOpen} onOpenChange={onOpenChange} isDismissable>
      <AlertDialog
        variant="destructive"
        actionLabel={t`Delete Account`}
        title={t`Delete Account`}
        onAction={() => onOpenChange(false)}
      >
        <Trans>
          You're about to permanently delete the account and all data environment through PlatformPlatform.
          <br />
          <br />
          This action is permanent and irreversible.
        </Trans>
      </AlertDialog>
    </Modal>
  );
}
