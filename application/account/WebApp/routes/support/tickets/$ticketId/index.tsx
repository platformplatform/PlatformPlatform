import { Trans } from "@lingui/react/macro";
import { Skeleton } from "@repo/ui/components/Skeleton";
import { useQueryClient } from "@tanstack/react-query";
import { Link as RouterLink, createFileRoute, useNavigate } from "@tanstack/react-router";
import { ArrowLeftIcon } from "lucide-react";
import { useEffect, useMemo, useRef, useState } from "react";

import { api, type Schemas, SupportTicketStatus } from "@/shared/lib/api/client";

import { CategoryPill } from "../../-components/CategoryPill";
import { ClosedTicketFooter } from "../../-components/ClosedTicketFooter";
import { InlineStatusEvent } from "../../-components/InlineStatusEvent";
import { MessageBubble } from "../../-components/MessageBubble";
import { ReplyComposer } from "../../-components/ReplyComposer";
import { StatusPill } from "../../-components/StatusPill";

type ThreadItem =
  | { kind: "message"; timestamp: string; message: Schemas["TicketMessageView"] }
  | { kind: "event"; timestamp: string; event: Schemas["TicketHistoryEventView"] };

export const Route = createFileRoute("/support/tickets/$ticketId/")({
  staticData: { trackingTitle: "Support ticket detail" },
  component: TicketDetailPage
});

function TicketDetailPage() {
  const { ticketId } = Route.useParams();
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const { data: ticket, isLoading } = api.useQuery("get", "/api/account/support-tickets/{id}", {
    params: { path: { id: ticketId } }
  });

  const scrollRef = useRef<HTMLDivElement>(null);
  // True while the user is composing a reopen reply. Cleared once the reply lands so the footer
  // returns to its "closed" state if the ticket is somehow still terminal (it won't be after a
  // successful reopen + reply, but the cleanup keeps the UI honest if anything regresses).
  const [reopenMode, setReopenMode] = useState(false);

  // Messages and lifecycle events render as a single chronological thread. Sorting by ISO-8601
  // timestamps is lexicographic-safe and stable, so events that happen at the exact same instant
  // as a message keep the backend's emission order.
  const threadItems = useMemo<ThreadItem[]>(() => {
    if (!ticket) return [];
    const messageItems: ThreadItem[] = ticket.messages.map((message) => ({
      kind: "message",
      timestamp: message.createdAt,
      message
    }));
    const eventItems: ThreadItem[] = ticket.historyEvents.map((event) => ({
      kind: "event",
      timestamp: event.occurredAt,
      event
    }));
    return [...messageItems, ...eventItems].sort((a, b) => a.timestamp.localeCompare(b.timestamp));
  }, [ticket]);

  useEffect(() => {
    if (scrollRef.current) {
      scrollRef.current.scrollTop = scrollRef.current.scrollHeight;
    }
  }, [threadItems.length]);

  if (isLoading) {
    return <TicketDetailSkeleton />;
  }

  if (!ticket) {
    return (
      <div className="flex flex-1 items-center justify-center text-muted-foreground">
        <Trans>Unable to load ticket.</Trans>
      </div>
    );
  }

  const invalidate = () => {
    queryClient.invalidateQueries({ queryKey: ["get", "/api/account/support-tickets/{id}"] });
    queryClient.invalidateQueries({ queryKey: ["get", "/api/account/support-tickets"] });
  };

  return (
    <div className="flex min-h-0 flex-1 flex-col">
      <TicketDetailHeader ticket={ticket} />
      <div ref={scrollRef} className="min-h-0 flex-1 overflow-y-auto px-4 sm:px-8">
        <div className="mx-auto flex w-full max-w-[48rem] flex-col gap-5 py-4">
          {threadItems.map((item, index) =>
            item.kind === "message" ? (
              <MessageBubble key={item.message.id} message={item.message} />
            ) : (
              <InlineStatusEvent key={`event-${item.timestamp}-${index}`} event={item.event} />
            )
          )}
        </div>
      </div>

      {(() => {
        const isTerminalStatus =
          ticket.status === SupportTicketStatus.Resolved || ticket.status === SupportTicketStatus.Closed;
        if (isTerminalStatus && !reopenMode) {
          return (
            <ClosedTicketFooter
              ticketId={ticketId}
              status={ticket.status}
              csat={ticket.csat}
              isCsatStale={ticket.isCsatStale}
              canBeReopened={ticket.canBeReopened}
              onStartReopen={() => setReopenMode(true)}
            />
          );
        }
        // Only chain /reopen before /reply when the ticket is still terminal at send-time. If a
        // staff reply lands while the user is composing, the polling refetch flips status back to
        // non-terminal and reopenMode would otherwise cause a redundant /reopen on an active ticket.
        return (
          <ReplyComposer
            ticketId={ticketId}
            reopenBeforeSend={reopenMode && isTerminalStatus}
            onResolved={() => {
              invalidate();
              setReopenMode(false);
              navigate({ to: "/support/tickets/$ticketId/close", params: { ticketId } });
            }}
            onSent={() => {
              invalidate();
              setReopenMode(false);
            }}
          />
        );
      })()}
    </div>
  );
}

function TicketDetailHeader({ ticket }: { ticket: Schemas["TicketDetailResponse"] }) {
  return (
    <div className="sticky top-0 z-10 border-b border-border bg-background px-4 pt-4 pb-3 sm:px-8">
      <div className="mx-auto flex w-full max-w-[48rem] flex-col">
        <RouterLink
          to="/support/tickets"
          className="inline-flex w-fit items-center gap-1.5 text-xs text-muted-foreground hover:text-foreground"
        >
          <ArrowLeftIcon className="size-3.5" aria-hidden={true} />
          <Trans>All tickets</Trans>
        </RouterLink>
        <div className="mt-2 flex flex-wrap items-center gap-3">
          <h1 className="flex-1">{ticket.subject}</h1>
          <StatusPill status={ticket.status} />
        </div>
        <div className="mt-1.5 flex flex-wrap items-center gap-2 text-xs text-muted-foreground">
          <CategoryPill category={ticket.category} />
          <span className="font-mono">#{ticket.shortDisplayId}</span>
        </div>
      </div>
    </div>
  );
}

function TicketDetailSkeleton() {
  return (
    <div className="flex min-h-0 flex-1 flex-col">
      <div className="border-b border-border px-4 pt-4 pb-3 sm:px-8">
        <div className="mx-auto flex w-full max-w-[48rem] flex-col gap-2">
          <Skeleton className="h-3 w-20" />
          <Skeleton className="h-7 w-2/3" />
          <Skeleton className="h-4 w-32" />
        </div>
      </div>
      <div className="min-h-0 flex-1 overflow-hidden px-4 sm:px-8">
        <div className="mx-auto flex w-full max-w-[48rem] flex-col gap-5 py-4">
          <Skeleton className="h-20 w-3/4" />
          <Skeleton className="ml-auto h-20 w-2/3" />
          <Skeleton className="h-16 w-2/3" />
        </div>
      </div>
    </div>
  );
}
