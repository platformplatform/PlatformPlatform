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

interface SupportDialogProps {
  isOpen: boolean;
  onOpenChange: (isOpen: boolean) => void;
}

export function SupportDialog({ isOpen, onOpenChange }: Readonly<SupportDialogProps>) {
  return (
    <Dialog open={isOpen} onOpenChange={onOpenChange}>
      <DialogContent className="sm:w-dialog-md">
        <DialogHeader>
          <DialogTitle>{t`Contact support`}</DialogTitle>
          <DialogDescription>{t`Need help? Our support team is here to assist you.`}</DialogDescription>
        </DialogHeader>
        <DialogBody>
          <TextField
            name="email"
            label={t`Email`}
            value="support@platformplatform.net"
            isReadOnly={true}
            startIcon={<MailIcon className="size-4" />}
          />
          <p className="text-muted-foreground text-sm">{t`Feel free to reach out with any questions or issues you may have.`}</p>
        </DialogBody>
        <DialogFooter>
          <DialogClose render={<Button autoFocus={true} />}>{t`Close`}</DialogClose>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
