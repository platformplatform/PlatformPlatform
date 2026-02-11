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
import { SubscriptionPlan } from "@/shared/lib/api/client";

type DowngradeConfirmationDialogProps = {
  isOpen: boolean;
  onOpenChange: (isOpen: boolean) => void;
  onConfirm: () => void;
  isPending: boolean;
  targetPlan: SubscriptionPlan;
  currentPeriodEnd: string | null;
};

export function DowngradeConfirmationDialog({
  isOpen,
  onOpenChange,
  onConfirm,
  isPending,
  targetPlan,
  currentPeriodEnd
}: Readonly<DowngradeConfirmationDialogProps>) {
  const formatLongDate = useFormatLongDate();
  const formattedDate = formatLongDate(currentPeriodEnd);

  const planName = targetPlan === SubscriptionPlan.Standard ? t`Standard` : t`Premium`;

  return (
    <AlertDialog open={isOpen} onOpenChange={onOpenChange} trackingTitle="Downgrade subscription">
      <AlertDialogContent>
        <AlertDialogHeader>
          <AlertDialogTitle>{t`Downgrade to ${planName}`}</AlertDialogTitle>
          <AlertDialogDescription>
            {formattedDate ? (
              <Trans>
                Your plan will be downgraded to {planName} at the end of your current billing period on {formattedDate}.
                You will keep your current plan features until then.
              </Trans>
            ) : (
              <Trans>
                Your plan will be downgraded to {planName} at the end of your current billing period. You will keep your
                current plan features until then.
              </Trans>
            )}
          </AlertDialogDescription>
        </AlertDialogHeader>
        <AlertDialogFooter>
          <AlertDialogCancel disabled={isPending}>{t`Cancel`}</AlertDialogCancel>
          <AlertDialogAction onClick={onConfirm} disabled={isPending}>
            {isPending ? t`Processing...` : t`Confirm downgrade`}
          </AlertDialogAction>
        </AlertDialogFooter>
      </AlertDialogContent>
    </AlertDialog>
  );
}
