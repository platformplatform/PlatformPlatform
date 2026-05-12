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
import { CheckCircle2Icon, ShieldAlertIcon } from "lucide-react";

export interface ArchivedAwaitingConfirmation {
  count: number;
  oldestOccurredAt: string;
  newestOccurredAt: string;
}

export interface ReplayArchivedResult {
  billingEventsAppended: number;
  replayedAt: string;
}

interface ConfirmProps {
  isOpen: boolean;
  onOpenChange: (open: boolean) => void;
  archivedAwaiting: ArchivedAwaitingConfirmation | null;
  onConfirm: () => void;
  onSkip: () => void;
}

export function ReplayArchivedConfirmDialog({
  isOpen,
  onOpenChange,
  archivedAwaiting,
  onConfirm,
  onSkip
}: Readonly<ConfirmProps>) {
  const formatDate = useFormatDate();
  return (
    <AlertDialog open={isOpen} onOpenChange={onOpenChange} trackingTitle="Disaster recovery confirm">
      <AlertDialogContent>
        <AlertDialogHeader>
          <AlertDialogMedia className="bg-red-100">
            <ShieldAlertIcon className="text-red-600" />
          </AlertDialogMedia>
          <AlertDialogTitle>
            <Trans>Disaster recovery from archived Stripe events?</Trans>
          </AlertDialogTitle>
          <AlertDialogDescription>
            {archivedAwaiting === null ? (
              <Trans>
                This rebuilds the billing event ledger from this tenant's archived Stripe payloads. It is a best-effort
                recovery that may produce incorrect subscription state or billing event rows. Only run it when standard
                Reconcile with Stripe has been tried and did not clear the drift.
              </Trans>
            ) : (
              <Trans>
                Reconcile found {archivedAwaiting.count} archived events older than Stripe's 30-day window, from{" "}
                {formatDate(archivedAwaiting.oldestOccurredAt)} to {formatDate(archivedAwaiting.newestOccurredAt)}. This
                is a best-effort recovery from locally stored payloads and may produce incorrect rows. Only run it when
                standard Reconcile with Stripe has been tried and did not clear the drift.
              </Trans>
            )}
          </AlertDialogDescription>
        </AlertDialogHeader>
        <AlertDialogFooter>
          <AlertDialogCancel variant="secondary" onClick={onSkip}>
            <Trans>Cancel</Trans>
          </AlertDialogCancel>
          <AlertDialogAction variant="destructive" onClick={onConfirm}>
            <Trans>Run disaster recovery</Trans>
          </AlertDialogAction>
        </AlertDialogFooter>
      </AlertDialogContent>
    </AlertDialog>
  );
}

interface ResultProps {
  isOpen: boolean;
  onOpenChange: (open: boolean) => void;
  result: ReplayArchivedResult | null;
}

export function ReplayArchivedResultDialog({ isOpen, onOpenChange, result }: Readonly<ResultProps>) {
  const formatDate = useFormatDate();
  return (
    <AlertDialog open={isOpen} onOpenChange={onOpenChange} trackingTitle="Disaster recovery result">
      <AlertDialogContent>
        <AlertDialogHeader>
          <AlertDialogMedia className="bg-emerald-100">
            <CheckCircle2Icon className="text-emerald-600" />
          </AlertDialogMedia>
          <AlertDialogTitle>
            <Trans>Disaster recovery complete</Trans>
          </AlertDialogTitle>
          <AlertDialogDescription>
            {result === null ? (
              <Trans>No result available.</Trans>
            ) : (
              <Trans>
                Replayed {result.billingEventsAppended} archived events into the billing event ledger at{" "}
                {formatDate(result.replayedAt)}.
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
