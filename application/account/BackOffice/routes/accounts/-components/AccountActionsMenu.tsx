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
import { Button } from "@repo/ui/components/Button";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger
} from "@repo/ui/components/DropdownMenu";
import { Tooltip, TooltipContent, TooltipTrigger } from "@repo/ui/components/Tooltip";
import { AlertTriangleIcon, ExternalLinkIcon, MoreVerticalIcon, RefreshCwIcon } from "lucide-react";
import { useState } from "react";

import { useMe } from "@/shared/hooks/useMe";
import { api } from "@/shared/lib/api/client";

import { ReconcileResultDialog, type ReconcileResult } from "./ReconcileResultDialog";
import {
  type ArchivedAwaitingConfirmation,
  ReplayArchivedConfirmDialog,
  ReplayArchivedResultDialog,
  type ReplayArchivedResult
} from "./ReplayArchivedDialogs";

interface AccountActionsMenuProps {
  tenantId: string;
  stripeCustomerUrl: string | null | undefined;
}

interface ReconcileApiResult extends ReconcileResult {
  archivedEventsAwaitingConfirmation: ArchivedAwaitingConfirmation | null;
}

export function AccountActionsMenu({ tenantId, stripeCustomerUrl }: Readonly<AccountActionsMenuProps>) {
  const { data: me } = useMe();
  const [result, setResult] = useState<ReconcileApiResult | null>(null);
  const [replayResult, setReplayResult] = useState<ReplayArchivedResult | null>(null);
  const [isResultOpen, setIsResultOpen] = useState(false);
  const [isConfirmOpen, setIsConfirmOpen] = useState(false);
  const [archivedAwaiting, setArchivedAwaiting] = useState<ArchivedAwaitingConfirmation | null>(null);
  const [isReplayConfirmOpen, setIsReplayConfirmOpen] = useState(false);
  const [isReplayResultOpen, setIsReplayResultOpen] = useState(false);

  const reconcileMutation = api.useMutation("post", "/api/back-office/tenants/{id}/reconcile-with-stripe", {
    onSuccess: (data) => {
      setResult(data);
      if (data.archivedEventsAwaitingConfirmation) {
        setArchivedAwaiting(data.archivedEventsAwaitingConfirmation);
        setIsReplayConfirmOpen(true);
      } else {
        setIsResultOpen(true);
      }
    }
  });

  const replayMutation = api.useMutation("post", "/api/back-office/tenants/{id}/replay-archived-stripe-events", {
    onSuccess: (data) => {
      setReplayResult(data);
      setIsReplayResultOpen(true);
    }
  });

  // Reconcile with Stripe is admin-only on the server (TenantsEndpoints.cs). Hide the trigger for
  // non-admins so the UI matches the policy.
  if (!me?.isAdmin) {
    return null;
  }

  const handleConfirmReconcile = () => {
    setIsConfirmOpen(false);
    reconcileMutation.mutate({ params: { path: { id: tenantId } } });
  };

  const handleConfirmReplay = () => {
    setIsReplayConfirmOpen(false);
    replayMutation.mutate({ params: { path: { id: tenantId } } });
  };

  const handleSkipReplay = () => {
    setIsReplayConfirmOpen(false);
    // Operator declined archive replay — surface the reconcile summary they would otherwise have seen,
    // so the underlying reconcile is not silently swallowed by the awaiting-confirmation branch.
    setIsResultOpen(true);
  };

  const handleRunDisasterRecovery = () => {
    // Operator-initiated escalation from the reconcile-result drift branch. There is no captured
    // archivedAwaiting snapshot in this flow — the dialog shows the no-snapshot copy that names the
    // best-effort caveat without quoting a count.
    setArchivedAwaiting(null);
    setIsResultOpen(false);
    setIsReplayConfirmOpen(true);
  };

  const isWorking = reconcileMutation.isPending || replayMutation.isPending;

  return (
    <>
      <DropdownMenu trackingTitle="Account actions">
        <Tooltip>
          <TooltipTrigger
            render={
              <DropdownMenuTrigger
                render={
                  <Button variant="outline" size="icon-sm" aria-label={t`Account actions`} disabled={isWorking}>
                    <MoreVerticalIcon className="size-4" />
                  </Button>
                }
              />
            }
          />
          <TooltipContent>{t`Account actions`}</TooltipContent>
        </Tooltip>
        <DropdownMenuContent align="end">
          <DropdownMenuItem
            trackingLabel="Reconcile with Stripe"
            onClick={() => setIsConfirmOpen(true)}
            disabled={isWorking}
          >
            <RefreshCwIcon className="size-4" />
            {reconcileMutation.isPending ? <Trans>Reconciling...</Trans> : <Trans>Reconcile with Stripe</Trans>}
          </DropdownMenuItem>
          {stripeCustomerUrl && (
            <DropdownMenuItem
              trackingLabel="Open in Stripe"
              onClick={() => window.open(stripeCustomerUrl, "_blank", "noopener,noreferrer")}
            >
              <ExternalLinkIcon className="size-4" />
              <Trans>Open in Stripe</Trans>
            </DropdownMenuItem>
          )}
        </DropdownMenuContent>
      </DropdownMenu>

      <AlertDialog open={isConfirmOpen} onOpenChange={setIsConfirmOpen} trackingTitle="Reconcile with Stripe confirm">
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogMedia className="bg-amber-100">
              <AlertTriangleIcon className="text-amber-600" />
            </AlertDialogMedia>
            <AlertDialogTitle>
              <Trans>Reconcile with Stripe?</Trans>
            </AlertDialogTitle>
            <AlertDialogDescription>
              <Trans>
                Reconcile rebuilds this tenant's subscription and billing events from Stripe directly using the
                events.list API (the last 30 days). If the drift is not cleared afterwards, disaster recovery from the
                locally archived Stripe payloads is available as a last resort.
              </Trans>
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel variant="secondary">
              <Trans>Cancel</Trans>
            </AlertDialogCancel>
            <AlertDialogAction onClick={handleConfirmReconcile}>
              <Trans>Reconcile</Trans>
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>

      <ReplayArchivedConfirmDialog
        isOpen={isReplayConfirmOpen}
        onOpenChange={setIsReplayConfirmOpen}
        archivedAwaiting={archivedAwaiting}
        onConfirm={handleConfirmReplay}
        onSkip={handleSkipReplay}
      />

      <ReconcileResultDialog
        isOpen={isResultOpen}
        onOpenChange={setIsResultOpen}
        result={result}
        onRunDisasterRecovery={handleRunDisasterRecovery}
      />

      <ReplayArchivedResultDialog
        isOpen={isReplayResultOpen}
        onOpenChange={setIsReplayResultOpen}
        result={replayResult}
      />
    </>
  );
}
