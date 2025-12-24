import { t } from "@lingui/core/macro";
import { Button } from "@repo/ui/components/Button";
import {
  Dialog,
  DialogClose,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle
} from "@repo/ui/components/Dialog";
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
        <div className="flex flex-col gap-4">
          <div className="flex items-center gap-3 rounded-lg border border-input bg-input-background p-4 opacity-50">
            <MailIcon className="h-5 w-5 text-muted-foreground" />
            <a href="mailto:support@platformplatform.net" className="text-primary hover:underline">
              support@platformplatform.net
            </a>
          </div>
          <p className="text-muted-foreground text-sm">{t`Feel free to reach out with any questions or issues you may have.`}</p>
        </div>
        <DialogFooter>
          <DialogClose render={<Button />}>{t`Close`}</DialogClose>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
