import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogMedia,
  AlertDialogTitle
} from "@repo/ui/components/AlertDialog";
import { AlertTriangleIcon } from "lucide-react";

type CancelSubscriptionDialogProps = {
  isOpen: boolean;
  onOpenChange: (isOpen: boolean) => void;
  onConfirm: () => void;
  isPending: boolean;
  currentPeriodEnd: string | null;
};

export function CancelSubscriptionDialog({
  isOpen,
  onOpenChange,
  onConfirm,
  isPending,
  currentPeriodEnd
}: Readonly<CancelSubscriptionDialogProps>) {
  const formattedDate = currentPeriodEnd
    ? new Date(currentPeriodEnd).toLocaleDateString(undefined, {
        year: "numeric",
        month: "long",
        day: "numeric"
      })
    : null;

  return (
    <AlertDialog open={isOpen} onOpenChange={onOpenChange}>
      <AlertDialogContent>
        <AlertDialogHeader>
          <AlertDialogMedia className="bg-destructive/10">
            <AlertTriangleIcon className="text-destructive" />
          </AlertDialogMedia>
          <AlertDialogTitle>{t`Cancel subscription`}</AlertDialogTitle>
          <AlertDialogDescription>
            {formattedDate ? (
              <Trans>
                Your subscription will remain active until {formattedDate}. After that, your account will be suspended
                and you will lose access to paid features.
              </Trans>
            ) : (
              <Trans>
                Your subscription will be cancelled at the end of the current billing period. After that, your account
                will be suspended.
              </Trans>
            )}
          </AlertDialogDescription>
        </AlertDialogHeader>
        <AlertDialogFooter>
          <AlertDialogCancel disabled={isPending}>{t`Keep subscription`}</AlertDialogCancel>
          <AlertDialogAction variant="destructive" onClick={onConfirm} disabled={isPending}>
            {isPending ? t`Cancelling...` : t`Cancel subscription`}
          </AlertDialogAction>
        </AlertDialogFooter>
      </AlertDialogContent>
    </AlertDialog>
  );
}
