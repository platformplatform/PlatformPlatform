import { t } from "@lingui/core/macro";
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
import { Button } from "@repo/ui/components/Button";
import { useFormatDate } from "@repo/ui/hooks/useSmartDate";
import { AlertTriangleIcon, CheckCircle2Icon, RefreshCwIcon } from "lucide-react";
import { useState } from "react";

import { api } from "@/shared/lib/api/client";

interface SyncWithStripeButtonProps {
  tenantId: string;
}

interface SyncResult {
  billingEventsAppended: number;
  hasDriftDetected: boolean;
  driftDiscrepancyCount: number;
  syncedAt: string;
}

export function SyncWithStripeButton({ tenantId }: Readonly<SyncWithStripeButtonProps>) {
  const formatDate = useFormatDate();
  const [result, setResult] = useState<SyncResult | null>(null);
  const [isResultOpen, setIsResultOpen] = useState(false);

  const syncMutation = api.useMutation("post", "/api/back-office/tenants/{id}/sync-with-stripe", {
    onSuccess: (data) => {
      setResult(data);
      setIsResultOpen(true);
    }
  });

  const handleClick = () => {
    syncMutation.mutate({ params: { path: { id: tenantId } } });
  };

  return (
    <>
      <Button
        variant="outline"
        size="sm"
        onClick={handleClick}
        isPending={syncMutation.isPending}
        aria-label={t`Sync with Stripe`}
      >
        {!syncMutation.isPending && <RefreshCwIcon className="size-4" />}
        {syncMutation.isPending ? <Trans>Syncing...</Trans> : <Trans>Sync with Stripe</Trans>}
      </Button>

      <AlertDialog open={isResultOpen} onOpenChange={setIsResultOpen} trackingTitle="Sync with Stripe result">
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
                <Trans>Sync complete with drift detected</Trans>
              ) : (
                <Trans>Sync complete</Trans>
              )}
            </AlertDialogTitle>
            <AlertDialogDescription>
              {result === null ? (
                <Trans>No result available.</Trans>
              ) : result.billingEventsAppended === 0 && !result.hasDriftDetected ? (
                <Trans>No new billing events were appended. Account state matches Stripe.</Trans>
              ) : result.billingEventsAppended > 0 ? (
                <Trans>
                  Appended {result.billingEventsAppended} new billing events. Last synced at{" "}
                  {formatDate(result.syncedAt)}.
                </Trans>
              ) : (
                <Trans>
                  Account has {result.driftDiscrepancyCount} drift discrepancies. Last synced at{" "}
                  {formatDate(result.syncedAt)}.
                </Trans>
              )}
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogAction onClick={() => setIsResultOpen(false)}>
              <Trans>Close</Trans>
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </>
  );
}
