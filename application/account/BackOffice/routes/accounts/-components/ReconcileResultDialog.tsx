import { Trans } from "@lingui/react/macro";
import {
  AlertDialog,
  AlertDialogAction,
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
}

export function ReconcileResultDialog({ isOpen, onOpenChange, result }: Readonly<Props>) {
  const formatDate = useFormatDate();
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
                {formatDate(result.reconciledAt)}.
              </Trans>
            )}
          </AlertDialogDescription>
        </AlertDialogHeader>
        <AlertDialogFooter>
          <AlertDialogAction onClick={() => onOpenChange(false)}>
            <Trans>Close</Trans>
          </AlertDialogAction>
        </AlertDialogFooter>
      </AlertDialogContent>
    </AlertDialog>
  );
}
