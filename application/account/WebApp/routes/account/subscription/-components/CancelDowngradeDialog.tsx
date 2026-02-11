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
import { useFormatLongDate } from "@repo/ui/hooks/useSmartDate";
import type { SubscriptionPlan } from "@/shared/lib/api/client";
import { getPlanLabel } from "@/shared/lib/api/subscriptionPlan";

type CancelDowngradeDialogProps = {
  isOpen: boolean;
  onOpenChange: (isOpen: boolean) => void;
  onConfirm: () => void;
  isPending: boolean;
  currentPlan: SubscriptionPlan;
  scheduledPlan: SubscriptionPlan;
  currentPeriodEnd: string | null;
};

export function CancelDowngradeDialog({
  isOpen,
  onOpenChange,
  onConfirm,
  isPending,
  currentPlan,
  scheduledPlan,
  currentPeriodEnd
}: Readonly<CancelDowngradeDialogProps>) {
  const formatLongDate = useFormatLongDate();
  const formattedDate = formatLongDate(currentPeriodEnd);

  const currentPlanName = getPlanLabel(currentPlan);
  const scheduledPlanName = getPlanLabel(scheduledPlan);

  return (
    <AlertDialog open={isOpen} onOpenChange={onOpenChange} trackingTitle="Cancel downgrade">
      <AlertDialogContent>
        <AlertDialogHeader>
          <AlertDialogTitle>{t`Cancel scheduled downgrade`}</AlertDialogTitle>
          <AlertDialogDescription>
            {formattedDate ? (
              <Trans>
                Your subscription is scheduled to downgrade from {currentPlanName} to {scheduledPlanName} on{" "}
                {formattedDate}. If you cancel the downgrade, you will stay on the {currentPlanName} plan.
              </Trans>
            ) : (
              <Trans>
                Your subscription is scheduled to downgrade from {currentPlanName} to {scheduledPlanName}. If you cancel
                the downgrade, you will stay on the {currentPlanName} plan.
              </Trans>
            )}
          </AlertDialogDescription>
        </AlertDialogHeader>
        <AlertDialogFooter>
          <AlertDialogCancel disabled={isPending}>{t`Keep downgrade`}</AlertDialogCancel>
          <AlertDialogAction onClick={onConfirm} disabled={isPending}>
            {isPending ? t`Processing...` : t`Cancel downgrade`}
          </AlertDialogAction>
        </AlertDialogFooter>
      </AlertDialogContent>
    </AlertDialog>
  );
}
