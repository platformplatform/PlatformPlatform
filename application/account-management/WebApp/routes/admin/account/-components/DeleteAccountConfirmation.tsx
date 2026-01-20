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
          <div className="flex items-center gap-3 rounded-lg border border-input bg-input-background p-4 opacity-50">
            <MailIcon className="size-5 text-muted-foreground" />
            <a href="mailto:support@platformplatform.net" className="text-primary hover:underline">
              support@platformplatform.net
            </a>
          </div>
          <p className="text-muted-foreground text-sm">{t`Our support team will assist you with the account deletion process and ensure all your data is properly removed.`}</p>
        </DialogBody>
        <DialogFooter>
          <DialogClose render={<Button />}>{t`Close`}</DialogClose>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
