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
import { useFormatDate } from "@repo/ui/hooks/useSmartDate";
import { AlertTriangleIcon, CheckCircle2Icon } from "lucide-react";

export interface ReconcileResult {
  billingEventsAppended: number;
  hasDriftDetected: boolean;
  driftDiscrepancyCount: number;
  reconciledAt: string;
}

interface Props {
  isOpen: boolean;
  onOpenChange: (open: boolean) => void;
  result: ReconcileResult | null;
  onRunDisasterRecovery?: () => void;
}

export function ReconcileResultDialog({ isOpen, onOpenChange, result, onRunDisasterRecovery }: Readonly<Props>) {
  const formatDate = useFormatDate();
  const showDisasterRecovery = result?.hasDriftDetected === true && onRunDisasterRecovery !== undefined;
  return (
    <AlertDialog open={isOpen} onOpenChange={onOpenChange} trackingTitle="Reconcile with Stripe result">
      <AlertDialogContent>
        <AlertDialogHeader>
          <AlertDialogMedia className={result?.hasDriftDetected ? "bg-amber-100" : "bg-emerald-100"}>
            {result?.hasDriftDetected ? (
              <AlertTriangleIcon className="text-amber-600" />
            ) : (
              <CheckCircle2Icon className="text-emerald-600" />
            )}
          </AlertDialogMedia>
          <AlertDialogTitle>
            {result?.hasDriftDetected ? (
              <Trans>Reconcile complete with drift detected</Trans>
            ) : (
              <Trans>Reconcile complete</Trans>
            )}
          </AlertDialogTitle>
          <AlertDialogDescription>
            {result === null ? (
              <Trans>No result available.</Trans>
            ) : result.billingEventsAppended === 0 && !result.hasDriftDetected ? (
              <Trans>No new billing events were appended. Account state matches Stripe.</Trans>
            ) : result.billingEventsAppended > 0 ? (
              <Trans>
                Appended {result.billingEventsAppended} new billing events. Last reconciled at{" "}
                {formatDate(result.reconciledAt)}.
              </Trans>
            ) : (
              <Trans>
                Account has {result.driftDiscrepancyCount} drift discrepancies. Last reconciled at{" "}
                {formatDate(result.reconciledAt)}. If standard reconcile cannot clear the drift, disaster recovery from
                archived Stripe events is available as a last resort.
              </Trans>
            )}
          </AlertDialogDescription>
        </AlertDialogHeader>
        <AlertDialogFooter>
          <AlertDialogCancel variant="secondary">
            <Trans>Close</Trans>
          </AlertDialogCancel>
          {showDisasterRecovery && (
            <AlertDialogAction variant="destructive" onClick={onRunDisasterRecovery}>
              <Trans>Run disaster recovery</Trans>
            </AlertDialogAction>
          )}
        </AlertDialogFooter>
      </AlertDialogContent>
    </AlertDialog>
  );
}
