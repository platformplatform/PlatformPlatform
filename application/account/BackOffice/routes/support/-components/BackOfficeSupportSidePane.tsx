import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { Button } from "@repo/ui/components/Button";
import { Separator } from "@repo/ui/components/Separator";
import { SidePane, SidePaneBody, SidePaneFooter, SidePaneHeader } from "@repo/ui/components/SidePane";
import { Skeleton } from "@repo/ui/components/Skeleton";
import { useNavigate } from "@tanstack/react-router";
import { ArrowRightIcon } from "lucide-react";
import { useEffect } from "react";

import { api, type Schemas } from "@/shared/lib/api/client";

import { AssignControls } from "./AssignControls";
import { CategoryPill } from "./CategoryPill";
import { HistoryTimeline } from "./HistoryTimeline";
import { AccountRow, ReporterRow } from "./LinkableSidePaneRows";
import { StaffCsatSummary } from "./StaffCsatSummary";
import { StatusPill } from "./StatusPill";

interface BackOfficeSupportSidePaneProps {
  ticketId: string | undefined;
  onClose?: () => void;
  mode: "preview" | "detail";
}

export function BackOfficeSupportSidePane({ ticketId, onClose, mode }: Readonly<BackOfficeSupportSidePaneProps>) {
  // Preview mode is a dismissable overlay sibling of the inbox table; detail mode is a permanent aside
  // alongside the chat thread (the route already represents an open ticket — no open/close semantics).
  if (mode === "detail") {
    return <DetailSidePane ticketId={ticketId} />;
  }
  return <PreviewSidePane ticketId={ticketId} onClose={onClose ?? (() => undefined)} />;
}

function PreviewSidePane({ ticketId, onClose }: { ticketId: string | undefined; onClose: () => void }) {
  const isOpen = ticketId !== undefined;
  const navigate = useNavigate();
  const { data: ticket, isLoading, isError } = useTicketDetail(ticketId, isOpen);

  // A stale or unauthorized selectedTicketId resolves to { isLoading: false, isError: true, data:
  // undefined }. Without this the pane would show the loading skeleton forever. The shared
  // errorHandler already surfaced a toast for the failed response; closing the pane clears the stale
  // URL parameter and gives a coherent "ticket isn't here" outcome.
  useEffect(() => {
    if (isError && !ticket) {
      onClose();
    }
  }, [isError, ticket, onClose]);

  return (
    <SidePane
      isOpen={isOpen}
      onOpenChange={(open) => !open && onClose()}
      trackingTitle="Support ticket preview"
      trackingKey={ticketId}
      aria-label={t`Support ticket preview`}
    >
      <SidePaneHeader closeButtonLabel={t`Close preview`}>
        {ticket ? <span className="truncate">{ticket.subject}</span> : <Trans>Support ticket</Trans>}
      </SidePaneHeader>
      <SidePaneBody className="flex flex-col gap-6">
        <SidePaneContent ticket={ticket} isLoading={isLoading} isError={isError} />
      </SidePaneBody>
      {ticket && (
        <SidePaneFooter className="flex flex-col gap-3 border-t border-border bg-card">
          <div className="flex items-stretch gap-2">
            <div className="flex flex-1 basis-0">
              <AssignControls ticketId={ticket.id} currentAssignee={ticket.assignee} />
            </div>
            <Button
              size="sm"
              className="flex-1 basis-0"
              onClick={() => navigate({ to: "/support/tickets/$ticketId", params: { ticketId: ticket.id } })}
            >
              <Trans>Open ticket</Trans>
              <ArrowRightIcon className="size-3.5" />
            </Button>
          </div>
        </SidePaneFooter>
      )}
    </SidePane>
  );
}

function DetailSidePane({ ticketId }: { ticketId: string | undefined }) {
  const { data: ticket, isLoading, isError } = useTicketDetail(ticketId, ticketId !== undefined);

  return (
    <aside
      className="flex h-full w-[var(--side-pane-width,24rem)] shrink-0 flex-col border-l border-border bg-card"
      aria-label={t`Support ticket details`}
    >
      <div className="flex h-16 shrink-0 items-center px-4">
        <h4 className="flex h-full items-center">
          {ticket ? <span className="truncate">{ticket.subject}</span> : <Trans>Support ticket</Trans>}
        </h4>
      </div>
      <div className="flex flex-1 flex-col gap-6 overflow-y-auto p-4">
        <SidePaneContent ticket={ticket} isLoading={isLoading} isError={isError} />
      </div>
      {ticket && (
        <div className="mt-auto flex flex-col gap-3 border-t border-border bg-card p-4 pb-[max(1rem,env(safe-area-inset-bottom))]">
          <AssignControls ticketId={ticket.id} currentAssignee={ticket.assignee} />
        </div>
      )}
    </aside>
  );
}

function useTicketDetail(ticketId: string | undefined, enabled: boolean) {
  return api.useQuery(
    "get",
    "/api/back-office/support-tickets/{id}",
    { params: { path: { id: ticketId ?? "" } } },
    { enabled }
  );
}

function SidePaneContent({
  ticket,
  isLoading,
  isError
}: {
  ticket: Schemas["StaffTicketDetailResponse"] | undefined;
  isLoading: boolean;
  isError: boolean;
}) {
  // The preview pane auto-closes on this same condition; rendering nothing avoids a flash of skeleton
  // before the close lands. The errorHandler toast already told the user the fetch failed.
  if (isError && !ticket) {
    return null;
  }
  if (isLoading || !ticket) {
    return (
      <div className="flex flex-col gap-4">
        <Skeleton className="h-6 w-3/4" />
        <Skeleton className="h-24 w-full" />
        <Skeleton className="h-24 w-full" />
      </div>
    );
  }
  return (
    <>
      <div className="flex flex-wrap items-center gap-2">
        <CategoryPill category={ticket.category} />
        <span className="font-mono text-xs text-muted-foreground">#{ticket.shortDisplayId}</span>
        <StatusPill status={ticket.status} />
      </div>

      <Section label={t`Reporter`}>
        <ReporterRow reporter={ticket.reporter} />
      </Section>

      <Separator />

      <Section label={t`Account`}>
        <AccountRow account={ticket.account} />
      </Section>

      {ticket.csat && (
        <>
          <Separator />
          <Section label={t`Rating`}>
            <StaffCsatSummary csat={ticket.csat} />
          </Section>
        </>
      )}

      {ticket.historyEvents.length > 0 && (
        <>
          <Separator />
          <Section label={t`History`}>
            <HistoryTimeline events={ticket.historyEvents} />
          </Section>
        </>
      )}
    </>
  );
}

function Section({ label, children }: { label: string; children: React.ReactNode }) {
  return (
    <section className="flex flex-col gap-2">
      <h4 className="text-xs font-semibold tracking-wider text-muted-foreground uppercase">{label}</h4>
      {children}
    </section>
  );
}
