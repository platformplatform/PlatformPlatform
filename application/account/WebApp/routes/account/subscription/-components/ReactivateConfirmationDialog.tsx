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
import { SubscriptionPlan } from "@/shared/lib/api/client";

type ReactivateConfirmationDialogProps = {
  isOpen: boolean;
  onOpenChange: (isOpen: boolean) => void;
  onConfirm: () => void;
  isPending: boolean;
  currentPlan: SubscriptionPlan;
  targetPlan: SubscriptionPlan;
};

export function ReactivateConfirmationDialog({
  isOpen,
  onOpenChange,
  onConfirm,
  isPending,
  currentPlan,
  targetPlan
}: Readonly<ReactivateConfirmationDialogProps>) {
  function getPlanLabel(plan: SubscriptionPlan): string {
    switch (plan) {
      case SubscriptionPlan.Basis:
        return t`Basis`;
      case SubscriptionPlan.Standard:
        return t`Standard`;
      case SubscriptionPlan.Premium:
        return t`Premium`;
    }
  }

  const planName = getPlanLabel(targetPlan);

  return (
    <AlertDialog open={isOpen} onOpenChange={onOpenChange} trackingTitle="Reactivate subscription">
      <AlertDialogContent>
        <AlertDialogHeader>
          <AlertDialogTitle>{t`Reactivate subscription`}</AlertDialogTitle>
          <AlertDialogDescription>
            {targetPlan === currentPlan ? (
              <Trans>Your cancellation will be reversed and your {planName} subscription will remain active.</Trans>
            ) : (
              <Trans>Your cancellation will be reversed and your subscription will be changed to {planName}.</Trans>
            )}
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
