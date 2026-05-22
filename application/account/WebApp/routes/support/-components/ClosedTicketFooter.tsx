import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { Button } from "@repo/ui/components/Button";
import { Link as RouterLink } from "@tanstack/react-router";
import { LockIcon, PenLineIcon, RotateCcwIcon } from "lucide-react";

import { type Schemas, SupportTicketCsatScore, SupportTicketStatus } from "@/shared/lib/api/client";

interface ClosedTicketFooterProps {
  ticketId: string;
  status: Schemas["SupportTicketStatus"];
  csat: Schemas["TicketCsatView"] | null;
  // True when the existing rating predates a later reopen; the user can submit a fresh one.
  isCsatStale: boolean;
  // True iff the backend will accept a /reopen call right now. False for Resolved tickets past the
  // 7-day window (see SupportTicket.CanBeReopenedAt). When false we show a "create a new ticket" CTA
  // instead of a Reopen button so the user has a path forward rather than a 400 toast.
  canBeReopened: boolean;
  // Click handler for the Reopen button. Does NOT call /reopen directly. The parent route switches
  // into reopen-compose mode and shows the ReplyComposer. The actual /reopen call piggybacks on the
  // first reply so a misclick on Reopen costs nothing and we always know WHY the ticket was reopened.
  onStartReopen: () => void;
}

// Replaces the ReplyComposer when a ticket has reached a terminal state (Resolved or Closed).
// Composing a reply on a closed thread is confusing. Instead surface the CSAT (either the existing
// rating or a prompt to give one) and a Reopen button that swaps the parent route into compose mode.
export function ClosedTicketFooter({
  ticketId,
  status,
  csat,
  isCsatStale,
  canBeReopened,
  onStartReopen
}: Readonly<ClosedTicketFooterProps>) {
  const isClosed = status === SupportTicketStatus.Closed;
  // Treat the rating as missing when stale so the user is prompted to share a fresh one.
  const showRating = csat !== null && !isCsatStale;

  return (
    <div className="border-t border-border bg-background px-4 py-4 sm:px-8 sm:py-5">
      <div className="mx-auto flex w-full max-w-[48rem] flex-col gap-3">
        {showRating && csat ? <CsatSummary csat={csat} /> : <CsatPrompt ticketId={ticketId} />}
        <div className="flex items-center gap-3 rounded-lg border border-border bg-muted/40 p-4">
          <LockIcon className="size-4 text-muted-foreground" aria-hidden={true} />
          <div className="flex-1 text-sm text-muted-foreground">
            {isClosed ? <Trans>This ticket is closed.</Trans> : <Trans>This ticket is marked as resolved.</Trans>}
          </div>
          {canBeReopened ? (
            <Button variant="outline" size="sm" onClick={onStartReopen}>
              <RotateCcwIcon className="size-3.5" />
              <Trans>Reopen ticket</Trans>
            </Button>
          ) : (
            <Button variant="outline" size="sm" render={<RouterLink to="/support/tickets/new" />}>
              <PenLineIcon className="size-3.5" />
              <Trans>New ticket</Trans>
            </Button>
          )}
        </div>
      </div>
    </div>
  );
}

function CsatSummary({ csat }: { csat: Schemas["TicketCsatView"] }) {
  const choice = csatDisplay[csat.score];
  return (
    <div className="flex items-start gap-3 rounded-lg border border-border bg-card p-4">
      <span className="text-3xl leading-none" aria-hidden={true}>
        {choice.emoji}
      </span>
      <div className="flex min-w-0 flex-1 flex-col gap-1">
        <span className="text-xs font-semibold tracking-wider text-muted-foreground uppercase">
          <Trans>Your rating</Trans>
        </span>
        <span className="text-sm font-medium">{choice.label()}</span>
        {csat.comment && <p className="text-sm text-muted-foreground">{csat.comment}</p>}
      </div>
    </div>
  );
}

function CsatPrompt({ ticketId }: { ticketId: string }) {
  return (
    <div className="flex items-center gap-3 rounded-lg border border-dashed border-border bg-card p-4">
      <div className="flex-1 text-sm">
        <span className="font-medium text-foreground">
          <Trans>How was the support you got?</Trans>
        </span>
        <span className="ml-1 text-muted-foreground">
          <Trans>It only takes a moment.</Trans>
        </span>
      </div>
      <Button
        variant="outline"
        size="sm"
        render={<RouterLink to="/support/tickets/$ticketId/close" params={{ ticketId }} />}
      >
        <Trans>Rate this support</Trans>
      </Button>
    </div>
  );
}

const csatDisplay: Record<Schemas["SupportTicketCsatScore"], { emoji: string; label: () => string }> = {
  [SupportTicketCsatScore.Helpful]: { emoji: "😊", label: () => t`Helpful` },
  [SupportTicketCsatScore.Ok]: { emoji: "😐", label: () => t`OK` },
  [SupportTicketCsatScore.NotGreat]: { emoji: "😠", label: () => t`Not great` }
};
