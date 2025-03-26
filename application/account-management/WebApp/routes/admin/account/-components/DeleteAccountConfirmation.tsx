import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { AlertDialog } from "@repo/ui/components/AlertDialog";
import { Modal } from "@repo/ui/components/Modal";

type DeleteAccountConfirmationProps = {
  isOpen: boolean;
  onOpenChange: (isOpen: boolean) => void;
};

export default function DeleteAccountConfirmation({ isOpen, onOpenChange }: Readonly<DeleteAccountConfirmationProps>) {
  return (
    <Modal isOpen={isOpen} onOpenChange={onOpenChange} isDismissable={true}>
      <AlertDialog
        variant="destructive"
        actionLabel={t`Delete account`}
        title={t`Delete account`}
        onAction={() => onOpenChange(false)}
      >
        <Trans>
          You are about to permanently delete the account and the entire data environment via PlatformPlatform.
          <br />
          <br />
          This action is permanent and irreversible.
        </Trans>
      </AlertDialog>
    </Modal>
  );
}
