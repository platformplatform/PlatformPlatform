import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { Dialog, DialogContent, DialogDescription, DialogHeader, DialogTitle } from "@repo/ui/components/Dialog";
import { LoaderCircleIcon } from "lucide-react";

type ProcessingPaymentModalProps = {
  isOpen: boolean;
};

export function ProcessingPaymentModal({ isOpen }: Readonly<ProcessingPaymentModalProps>) {
  return (
    <Dialog open={isOpen} onOpenChange={() => {}} trackingTitle="Processing payment">
      <DialogContent className="sm:w-dialog-md" aria-label={t`Processing payment`}>
        <DialogHeader>
          <DialogTitle>{t`Processing payment`}</DialogTitle>
          <DialogDescription>
            <Trans>Please wait while we confirm your payment. This may take a few moments.</Trans>
          </DialogDescription>
        </DialogHeader>
        <div className="flex items-center justify-center py-8">
          <LoaderCircleIcon className="size-8 animate-spin text-primary" />
        </div>
      </DialogContent>
    </Dialog>
  );
}
