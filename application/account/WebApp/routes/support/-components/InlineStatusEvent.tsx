import { Trans } from "@lingui/react/macro";
import { useFormatDate } from "@repo/ui/hooks/useSmartDate";
import { CheckCircle2Icon, RotateCcwIcon } from "lucide-react";

import { type Schemas, TicketUserVisibleEventType } from "@/shared/lib/api/client";

type TicketHistoryEvent = Schemas["TicketHistoryEventView"];

export function InlineStatusEvent({ event }: Readonly<{ event: TicketHistoryEvent }>) {
  // Absolute timestamp so a user scanning a reopened-and-re-resolved thread can see when each
  // lifecycle change happened, mirroring the message bubble timestamps that frame them.
  const formatDate = useFormatDate();
  const isResolved = event.type === TicketUserVisibleEventType.Resolved;
  const Icon = isResolved ? CheckCircle2Icon : RotateCcwIcon;

  return (
    <div className="flex items-center justify-center">
      <div className="flex items-center gap-2 rounded-full border border-border bg-card px-3 py-1 text-xs text-muted-foreground">
        <Icon className="size-3.5" aria-hidden={true} />
        <span>
          {isResolved ? (
            <Trans>
              <span className="font-medium text-foreground">{event.actorDisplayName}</span> marked this ticket as
              resolved
            </Trans>
          ) : (
            <Trans>
              <span className="font-medium text-foreground">{event.actorDisplayName}</span> reopened this ticket
            </Trans>
          )}
        </span>
        <span aria-hidden={true}>·</span>
        <span className="tabular-nums">{formatDate(event.occurredAt, true, true)}</span>
      </div>
    </div>
  );
}
