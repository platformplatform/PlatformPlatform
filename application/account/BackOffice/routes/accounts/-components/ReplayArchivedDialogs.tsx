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
    <AlertDialog open={isOpen} onOpenChange={onOpenChange} trackingTitle="Replay archived Stripe events confirm">
      <AlertDialogContent>
        <AlertDialogHeader>
          <AlertDialogMedia className="bg-amber-100">
            <AlertTriangleIcon className="text-amber-600" />
          </AlertDialogMedia>
          <AlertDialogTitle>
            <Trans>Replay archived Stripe events?</Trans>
          </AlertDialogTitle>
          <AlertDialogDescription>
            {archivedAwaiting === null ? (
              <Trans>No archived events found.</Trans>
            ) : (
              <Trans>
                Reconcile found {archivedAwaiting.count} archived events older than Stripe's 30-day window, from{" "}
                {formatDate(archivedAwaiting.oldestOccurredAt)} to {formatDate(archivedAwaiting.newestOccurredAt)}.
                Replaying writes them into the billing event ledger using the locally stored payload, which may be
                approximate. Confirm only if you have reviewed the records in Stripe Dashboard.
              </Trans>
            )}
          </AlertDialogDescription>
        </AlertDialogHeader>
        <AlertDialogFooter>
          <AlertDialogCancel variant="secondary" onClick={onSkip}>
            <Trans>Skip replay</Trans>
          </AlertDialogCancel>
          <AlertDialogAction onClick={onConfirm}>
            <Trans>Replay archive</Trans>
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
    <AlertDialog open={isOpen} onOpenChange={onOpenChange} trackingTitle="Replay archived Stripe events result">
      <AlertDialogContent>
        <AlertDialogHeader>
          <AlertDialogMedia className="bg-emerald-100">
            <CheckCircle2Icon className="text-emerald-600" />
          </AlertDialogMedia>
          <AlertDialogTitle>
            <Trans>Archive replay complete</Trans>
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
