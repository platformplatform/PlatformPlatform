import { plural, t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { AppLayout } from "@repo/ui/components/AppLayout";
import { Button } from "@repo/ui/components/Button";
import { Empty, EmptyDescription, EmptyHeader, EmptyMedia, EmptyTitle } from "@repo/ui/components/Empty";
import { Skeleton } from "@repo/ui/components/Skeleton";
import { Link as RouterLink, createFileRoute, useNavigate } from "@tanstack/react-router";
import {
  CalendarIcon,
  ChevronDownIcon,
  ChevronRightIcon,
  MessageCirclePlusIcon,
  MessageSquareWarningIcon,
  MessagesSquareIcon,
  PaperclipIcon,
  PenLineIcon
} from "lucide-react";
import { useState } from "react";

import { SmartDate } from "@/shared/components/SmartDate";
import { api, type Schemas, SupportTicketStatus } from "@/shared/lib/api/client";

import { CategoryPill } from "../-components/CategoryPill";
import { StatusPill } from "../-components/StatusPill";

type TicketSummary = Schemas["MyTicketSummary"];

export const Route = createFileRoute("/support/tickets/")({
  staticData: { trackingTitle: "My support tickets" },
  component: MyTicketsPage
});

function MyTicketsPage() {
  const { data, isLoading } = api.useQuery("get", "/api/account/support-tickets");
  const navigate = useNavigate({ from: Route.fullPath });

  const active = data?.active ?? [];
  const closed = data?.closed ?? [];
  const awaitingUserCount = data?.awaitingUserCount ?? 0;
  const firstAwaitingReply = active.find((ticket) => ticket.status === SupportTicketStatus.AwaitingUser);
  const populated = active.length > 0 || closed.length > 0;
  const conversationsLabel = t`${active.length} active conversations. We usually reply within a day.`;
  const oneConversationLabel = t`1 active conversation. We usually reply within a day.`;
  const subtitle = populated
    ? active.length === 1
      ? oneConversationLabel
      : conversationsLabel
    : t`Need a hand? Open a ticket and someone from support will get back to you.`;

  if (isLoading) {
    return (
      <AppLayout variant="center" maxWidth="64rem" title={t`Support tickets`}>
        <div className="mt-6 flex flex-col gap-2.5">
          {Array.from({ length: 3 }).map((_, index) => (
            <Skeleton key={`ticket-skeleton-${index}`} className="h-24 w-full rounded-lg" />
          ))}
        </div>
      </AppLayout>
    );
  }

  return (
    <AppLayout variant="center" maxWidth="64rem" title={t`Support tickets`} subtitle={subtitle}>
      <div className="flex justify-end">
        {populated && (
          <Button onClick={() => navigate({ to: "/support/tickets/new" })}>
            <PenLineIcon />
            <Trans>Ask the team</Trans>
          </Button>
        )}
      </div>

      {!populated && (
        <Empty className="mt-6">
          <EmptyHeader>
            <EmptyMedia variant="icon">
              <MessageCirclePlusIcon />
            </EmptyMedia>
            <EmptyTitle>
              <Trans>No tickets yet</Trans>
            </EmptyTitle>
            <EmptyDescription>
              <Trans>
                When you ask the team for help, your conversations will live here. We typically reply within a business
                day.
              </Trans>
            </EmptyDescription>
          </EmptyHeader>
          <Button onClick={() => navigate({ to: "/support/tickets/new" })}>
            <PenLineIcon />
            <Trans>Open your first ticket</Trans>
          </Button>
        </Empty>
      )}

      {populated && awaitingUserCount > 0 && firstAwaitingReply && (
        <div className="mt-6 flex items-center gap-3 rounded-lg border border-primary/30 bg-primary/5 p-4">
          <span className="flex size-9 shrink-0 items-center justify-center rounded-md bg-primary/15 text-primary">
            <MessageSquareWarningIcon className="size-5" aria-hidden={true} />
          </span>
          <div className="min-w-0 flex-1">
            <div className="text-sm font-semibold">
              {plural(awaitingUserCount, {
                one: "# ticket needs your reply",
                other: "# tickets need your reply"
              })}
            </div>
            <div className="truncate text-xs text-muted-foreground">{firstAwaitingReply.subject}</div>
          </div>
          <Button
            variant="outline"
            size="sm"
            render={<RouterLink to="/support/tickets/$ticketId" params={{ ticketId: firstAwaitingReply.id }} />}
          >
            <Trans>View</Trans>
          </Button>
        </div>
      )}

      {populated && (
        <div className="mt-6 flex flex-col gap-4">
          <SectionLabel>
            <Trans>Active</Trans>
          </SectionLabel>
          <div className="flex flex-col gap-2.5">
            {active.length === 0 ? (
              <p className="text-sm text-muted-foreground">
                <Trans>No active tickets right now.</Trans>
              </p>
            ) : (
              active.map((ticket) => <TicketCard key={ticket.id} ticket={ticket} />)
            )}
          </div>

          <ClosedSection closed={closed} />
        </div>
      )}
    </AppLayout>
  );
}

function SectionLabel({ children }: { children: React.ReactNode }) {
  return <div className="text-xs font-semibold tracking-wider text-muted-foreground uppercase">{children}</div>;
}

function TicketCard({ ticket }: { ticket: TicketSummary }) {
  return (
    <RouterLink
      to="/support/tickets/$ticketId"
      params={{ ticketId: ticket.id }}
      className="flex flex-col gap-2 rounded-lg border border-border bg-card px-5 py-4 text-left no-underline outline-ring transition-colors hover:bg-muted/50 hover:no-underline focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 active:bg-muted"
    >
      <div className="flex flex-wrap items-center gap-2">
        <CategoryPill category={ticket.category} />
        <span className="font-mono text-xs text-muted-foreground">#{ticket.shortDisplayId}</span>
        <div className="flex-1" />
        <StatusPill status={ticket.status} />
      </div>
      <h3 className="text-base leading-tight font-semibold">{ticket.subject}</h3>
      <div className="flex flex-wrap items-center gap-3.5 text-xs text-muted-foreground">
        <span className="inline-flex items-center gap-1">
          <CalendarIcon className="size-3" aria-hidden={true} />
          <SmartDate date={ticket.createdAt} />
        </span>
        <span className="inline-flex items-center gap-1">
          <MessagesSquareIcon className="size-3" aria-hidden={true} />
          {ticket.messagesCount}
        </span>
        {ticket.attachmentsCount > 0 && (
          <span className="inline-flex items-center gap-1">
            <PaperclipIcon className="size-3" aria-hidden={true} />
            {ticket.attachmentsCount}
          </span>
        )}
        <div className="flex-1" />
        <SmartDate date={ticket.lastActivityAt} />
      </div>
    </RouterLink>
  );
}

function ClosedSection({ closed }: { closed: TicketSummary[] }) {
  const [isExpanded, setIsExpanded] = useState(false);
  if (closed.length === 0) return null;
  return (
    <div className="mt-2 flex flex-col gap-2 border-t border-border pt-4">
      <Button
        variant="ghost"
        onClick={() => setIsExpanded((value) => !value)}
        className="h-auto w-full justify-start gap-2 px-0 text-xs font-semibold tracking-wider text-muted-foreground uppercase hover:bg-transparent"
        aria-expanded={isExpanded}
      >
        {isExpanded ? <ChevronDownIcon className="size-3.5" /> : <ChevronRightIcon className="size-3.5" />}
        <Trans>Closed ({closed.length})</Trans>
      </Button>
      {isExpanded && (
        <div className="flex flex-col gap-2 opacity-90">
          {closed.map((ticket) => (
            <TicketCard key={ticket.id} ticket={ticket} />
          ))}
        </div>
      )}
    </div>
  );
}
