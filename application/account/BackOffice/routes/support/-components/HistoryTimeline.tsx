import { msg } from "@lingui/core/macro";
import { useLingui } from "@lingui/react";
import { Trans } from "@lingui/react/macro";
import { useFormatDate } from "@repo/ui/hooks/useSmartDate";
import { PaperclipIcon } from "lucide-react";

import { type Schemas, SupportTicketHistoryEventType } from "@/shared/lib/api/client";

type StaffHistoryEvent = Schemas["StaffTicketHistoryEventView"];

const eventLabels = {
  [SupportTicketHistoryEventType.Created]: msg`opened this ticket`,
  [SupportTicketHistoryEventType.MessagePosted]: msg`posted a message`,
  [SupportTicketHistoryEventType.StatusChanged]: msg`changed status`,
  [SupportTicketHistoryEventType.AssigneeChanged]: msg`changed assignee`,
  [SupportTicketHistoryEventType.CsatSubmitted]: msg`submitted a satisfaction rating`,
  [SupportTicketHistoryEventType.Reopened]: msg`reopened this ticket`
};

export function HistoryTimeline({ events }: Readonly<{ events: StaffHistoryEvent[] }>) {
  const { i18n } = useLingui();
  // Absolute timestamps so staff can diagnose the lifecycle out of order — relative "3 hours ago"
  // hides whether two events happened minutes or hours apart. Year drops on current-year events to
  // stay scannable but reappears automatically for older history.
  const formatDate = useFormatDate();
  if (events.length === 0) {
    return (
      <span className="text-xs text-muted-foreground">
        <Trans>No history yet.</Trans>
      </span>
    );
  }
  return (
    <div className="relative flex flex-col gap-3 border-l border-border pl-4" role="list">
      {events.map((event, index) => {
        const label = i18n._(eventLabels[event.type]);
        return (
          <div key={`${event.occurredAt}-${index}`} role="listitem" className="relative">
            <span className="absolute top-1.5 -left-5 size-2 rounded-full bg-muted-foreground" aria-hidden={true} />
            <div className="text-xs text-foreground">
              <span className="font-medium">{event.actorDisplayName}</span>
              <span className="text-muted-foreground"> {label}</span>
              {event.payload && <span className="text-muted-foreground"> · {event.payload}</span>}
            </div>
            {event.hasAttachment && (
              <div className="mt-1 inline-flex items-center gap-1 text-xs text-muted-foreground">
                <PaperclipIcon className="size-3" aria-hidden={true} />
                <Trans>Attachment included</Trans>
              </div>
            )}
            <div className="mt-0.5 text-xs text-muted-foreground tabular-nums">
              {formatDate(event.occurredAt, true, true)}
            </div>
          </div>
        );
      })}
    </div>
  );
}
