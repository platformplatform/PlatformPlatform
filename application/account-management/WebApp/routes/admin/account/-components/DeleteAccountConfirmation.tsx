import { t } from "@lingui/core/macro";
import { Button } from "@repo/ui/components/Button";
import { Dialog } from "@repo/ui/components/Dialog";
import { Heading } from "@repo/ui/components/Heading";
import { Modal } from "@repo/ui/components/Modal";
import { MailIcon, XIcon } from "lucide-react";

type DeleteAccountConfirmationProps = {
  isOpen: boolean;
  onOpenChange: (isOpen: boolean) => void;
};

export default function DeleteAccountConfirmation({ isOpen, onOpenChange }: Readonly<DeleteAccountConfirmationProps>) {
  return (
    <Modal isOpen={isOpen} onOpenChange={onOpenChange} isDismissable={true} zIndex="high">
      <Dialog className="max-w-lg">
        {({ close }) => (
          <>
            <XIcon onClick={close} className="absolute top-2 right-2 h-10 w-10 cursor-pointer p-2 hover:bg-muted" />
            <Heading slot="title" className="text-2xl">
              {t`Delete account`}
            </Heading>
            <p className="text-muted-foreground text-sm">{t`To delete your account, please contact our support team.`}</p>
            <div className="mt-4 flex flex-col gap-4">
              <div className="flex items-center gap-3 rounded-lg border border-input bg-input-background p-4 opacity-50">
                <MailIcon className="h-5 w-5 text-muted-foreground" />
                <a href="mailto:support@platformplatform.net" className="text-primary hover:underline">
                  support@platformplatform.net
                </a>
              </div>
              <p className="text-muted-foreground text-sm">{t`Our support team will assist you with the account deletion process and ensure all your data is properly removed.`}</p>
              <div className="mt-6 flex justify-end gap-4">
                <Button onPress={close}>{t`Close`}</Button>
              </div>
            </div>
          </>
        )}
      </Dialog>
    </Modal>
  );
}
