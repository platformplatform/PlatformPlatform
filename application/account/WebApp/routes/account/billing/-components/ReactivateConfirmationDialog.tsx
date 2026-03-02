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
  AlertDialogTitle
} from "@repo/ui/components/AlertDialog";
import type { SubscriptionPlan } from "@/shared/lib/api/client";
import { getPlanLabel } from "@/shared/lib/api/subscriptionPlan";

type ReactivateConfirmationDialogProps = {
  isOpen: boolean;
  onOpenChange: (isOpen: boolean) => void;
  onConfirm: () => void;
  isPending: boolean;
  currentPlan: SubscriptionPlan;
};

export function ReactivateConfirmationDialog({
  isOpen,
  onOpenChange,
  onConfirm,
  isPending,
  currentPlan
}: Readonly<ReactivateConfirmationDialogProps>) {
  const planName = getPlanLabel(currentPlan);

  return (
    <AlertDialog open={isOpen} onOpenChange={onOpenChange} trackingTitle="Reactivate subscription">
      <AlertDialogContent>
        <AlertDialogHeader>
          <AlertDialogTitle>{t`Reactivate subscription`}</AlertDialogTitle>
          <AlertDialogDescription>
            <Trans>Your cancellation will be reversed and your {planName} subscription will remain active.</Trans>
          </AlertDialogDescription>
        </AlertDialogHeader>
        <AlertDialogFooter>
          <AlertDialogCancel disabled={isPending}>{t`Cancel`}</AlertDialogCancel>
          <AlertDialogAction onClick={onConfirm} disabled={isPending}>
            {isPending ? t`Processing...` : t`Reactivate`}
          </AlertDialogAction>
        </AlertDialogFooter>
      </AlertDialogContent>
    </AlertDialog>
  );
}
