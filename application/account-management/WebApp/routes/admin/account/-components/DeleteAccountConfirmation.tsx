import { t } from "@lingui/core/macro";
import { Button } from "@repo/ui/components/Button";
import {
  Dialog,
  DialogBody,
  DialogClose,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle
} from "@repo/ui/components/Dialog";
import { TextField } from "@repo/ui/components/TextField";
import { MailIcon } from "lucide-react";

type DeleteAccountConfirmationProps = {
  isOpen: boolean;
  onOpenChange: (isOpen: boolean) => void;
};

export default function DeleteAccountConfirmation({ isOpen, onOpenChange }: Readonly<DeleteAccountConfirmationProps>) {
  return (
    <Dialog open={isOpen} onOpenChange={onOpenChange}>
      <DialogContent className="sm:w-dialog-md">
        <DialogHeader>
          <DialogTitle>{t`Delete account`}</DialogTitle>
          <DialogDescription>{t`To delete your account, please contact our support team.`}</DialogDescription>
        </DialogHeader>
        <DialogBody>
          <TextField
            name="email"
            label={t`Email`}
            value="support@platformplatform.net"
            isReadOnly={true}
            startIcon={<MailIcon className="size-4" />}
          />
          <p className="text-muted-foreground text-sm">{t`Our support team will assist you with the account deletion process and ensure all your data is properly removed.`}</p>
        </DialogBody>
        <DialogFooter>
          <DialogClose render={<Button autoFocus={true} />}>{t`Close`}</DialogClose>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
