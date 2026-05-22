import { t } from "@lingui/core/macro";
import { requireSupportSystemEnabled } from "@repo/infrastructure/auth/routeGuards";
import { Button } from "@repo/ui/components/Button";
import { SidebarInset, SidebarProvider } from "@repo/ui/components/Sidebar";
import { Skeleton } from "@repo/ui/components/Skeleton";
import { Tooltip, TooltipContent, TooltipTrigger } from "@repo/ui/components/Tooltip";
import { createFileRoute, useRouter } from "@tanstack/react-router";
import { ArrowLeftIcon } from "lucide-react";
import { Fragment, useEffect, useMemo, useRef } from "react";

import { BackOfficeSideMenu } from "@/shared/components/BackOfficeSideMenu";
import { NotFoundPage } from "@/shared/components/errorPages/NotFoundPage";
import { api, type Schemas } from "@/shared/lib/api/client";

import { BackOfficeSupportSidePane } from "../-components/BackOfficeSupportSidePane";
import { CategoryPill } from "../-components/CategoryPill";
import { MessageBubble } from "../-components/MessageBubble";
import { StaffCsatSummary } from "../-components/StaffCsatSummary";
import { StaffReplyComposer } from "../-components/StaffReplyComposer";
import { StatusPill } from "../-components/StatusPill";

export const Route = createFileRoute("/support/tickets/$ticketId")({
  staticData: { trackingTitle: "Support ticket" },
  beforeLoad: () => requireSupportSystemEnabled(),
  component: SupportTicketDetailPage
});

function SupportTicketDetailPage() {
  const { ticketId } = Route.useParams();

  const {
    data: ticket,
    isLoading,
    isError
  } = api.useQuery("get", "/api/back-office/support-tickets/{id}", {
    params: { path: { id: ticketId } }
  });

  // Any non-success outcome (404 not-found, 400 malformed ID, 5xx, network) renders the standard
  // not-found page rather than holding the user on a perpetual skeleton.
  if (isError && !ticket) {
    return <NotFoundPage />;
  }

  return (
    <SidebarProvider>
      <BackOfficeSideMenu />
      <SidebarInset>
        <div className="flex h-full min-h-0 flex-1 flex-row">
          <div className="flex min-h-0 flex-1 flex-col">
            {isLoading || !ticket ? <TicketDetailSkeleton /> : <TicketDetailBody ticket={ticket} />}
          </div>
          <BackOfficeSupportSidePane ticketId={ticket?.id} mode="detail" />
        </div>
      </SidebarInset>
    </SidebarProvider>
  );
}

type ChatEntry =
  | { kind: "message"; key: string; timestamp: string; message: Schemas["StaffTicketMessageView"] }
  | { kind: "csat"; key: string; timestamp: string; csat: Schemas["TicketCsatView"] };

function TicketDetailBody({ ticket }: { ticket: Schemas["StaffTicketDetailResponse"] }) {
  const scrollRef = useRef<HTMLDivElement>(null);

  // Interleave messages and the CSAT card by timestamp so the rating appears at its actual
  // chronological position relative to the surrounding conversation. Messages use `postedAt`,
  // CSAT uses `submittedAt`. Stable sort keeps server-provided message order on ties.
  const chatEntries = useMemo<ChatEntry[]>(() => {
    const entries: ChatEntry[] = ticket.messages.map((message) => ({
      kind: "message",
      key: `message-${message.id}`,
      timestamp: message.postedAt,
      message
    }));
    if (ticket.csat) {
      entries.push({ kind: "csat", key: "csat", timestamp: ticket.csat.submittedAt, csat: ticket.csat });
    }
    return entries.sort((a, b) => a.timestamp.localeCompare(b.timestamp));
  }, [ticket.messages, ticket.csat]);

  useEffect(() => {
    if (scrollRef.current) {
      scrollRef.current.scrollTop = scrollRef.current.scrollHeight;
    }
  }, [chatEntries.length]);

  return (
    <div className="flex min-h-0 flex-1 flex-col">
      <TicketDetailHeader ticket={ticket} />
      <div ref={scrollRef} className="min-h-0 flex-1 overflow-y-auto px-4 sm:px-8">
        <div className="mx-auto flex w-full max-w-[48rem] flex-col gap-5 py-4">
          {chatEntries.map((entry) => (
            <Fragment key={entry.key}>
              {entry.kind === "message" ? (
                <MessageBubble message={entry.message} reporterAvatarUrl={ticket.reporter.avatarUrl} />
              ) : (
                <StaffCsatSummary csat={entry.csat} />
              )}
            </Fragment>
          ))}
        </div>
      </div>
      <StaffReplyComposer ticketId={ticket.id} status={ticket.status} />
    </div>
  );
}

function TicketDetailHeader({ ticket }: { ticket: Schemas["StaffTicketDetailResponse"] }) {
  const router = useRouter();
  // Browser history is the source of truth for "back". This preserves the inbox's exact search,
  // filter, sort, and selection state without storing returnPath on the URL or in localStorage.
  // If the user landed here from a deep link with no history, fall back to the inbox root.
  const handleBack = () => {
    if (window.history.length > 1) {
      router.history.back();
    } else {
      router.navigate({ to: "/support/tickets" });
    }
  };

  return (
    <div className="sticky top-0 z-10 border-b border-border bg-background px-4 pt-4 pb-3 sm:px-8">
      <div className="mx-auto flex w-full max-w-[48rem] flex-col">
        <div className="flex flex-wrap items-center gap-3">
          <Tooltip>
            <TooltipTrigger
              render={
                <Button variant="ghost" size="icon-sm" onClick={handleBack} aria-label={t`Back to inbox`}>
                  <ArrowLeftIcon className="size-5" aria-hidden={true} />
                </Button>
              }
            />
            <TooltipContent>{t`Back to inbox`}</TooltipContent>
          </Tooltip>
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
