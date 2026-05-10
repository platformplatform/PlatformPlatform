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
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger
} from "@repo/ui/components/DropdownMenu";
import { Tooltip, TooltipContent, TooltipTrigger } from "@repo/ui/components/Tooltip";
import { useFormatDate } from "@repo/ui/hooks/useSmartDate";
import { AlertTriangleIcon, CheckCircle2Icon, ExternalLinkIcon, MoreVerticalIcon, RefreshCwIcon } from "lucide-react";
import { useState } from "react";

import { useMe } from "@/shared/hooks/useMe";
import { api } from "@/shared/lib/api/client";

interface AccountActionsMenuProps {
  tenantId: string;
  stripeCustomerUrl: string | null | undefined;
}

interface SyncResult {
  billingEventsAppended: number;
  hasDriftDetected: boolean;
  driftDiscrepancyCount: number;
  syncedAt: string;
}

export function AccountActionsMenu({ tenantId, stripeCustomerUrl }: Readonly<AccountActionsMenuProps>) {
  const formatDate = useFormatDate();
  const { data: me } = useMe();
  const [result, setResult] = useState<SyncResult | null>(null);
  const [isResultOpen, setIsResultOpen] = useState(false);

  const syncMutation = api.useMutation("post", "/api/back-office/tenants/{id}/sync-with-stripe", {
    onSuccess: (data) => {
      setResult(data);
      setIsResultOpen(true);
    }
  });

  const handleSync = () => {
    syncMutation.mutate({ params: { path: { id: tenantId } } });
  };

  // Sync with Stripe is admin-only on the server (TenantsEndpoints.cs). Hide the trigger for
  // non-admins so the UI matches the policy.
  if (!me?.isAdmin) {
    return null;
  }

  return (
    <>
      <DropdownMenu trackingTitle="Account actions">
        <Tooltip>
          <TooltipTrigger
            render={
              <DropdownMenuTrigger
                render={
                  <Button
                    variant="outline"
                    size="icon-sm"
                    aria-label={t`Account actions`}
                    disabled={syncMutation.isPending}
                  >
                    <MoreVerticalIcon className="size-4" />
                  </Button>
                }
              />
            }
          />
          <TooltipContent>{t`Account actions`}</TooltipContent>
        </Tooltip>
        <DropdownMenuContent align="end">
          <DropdownMenuItem trackingLabel="Sync with Stripe" onClick={handleSync} disabled={syncMutation.isPending}>
            <RefreshCwIcon className="size-4" />
            {syncMutation.isPending ? <Trans>Syncing...</Trans> : <Trans>Sync with Stripe</Trans>}
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
