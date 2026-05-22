import { Trans } from "@lingui/react/macro";
import { Button } from "@repo/ui/components/Button";
import { Skeleton } from "@repo/ui/components/Skeleton";
import { useQueryClient } from "@tanstack/react-query";
import { Link as RouterLink, createFileRoute, useNavigate } from "@tanstack/react-router";
import { ArrowLeftIcon, CheckCircle2Icon, LockIcon } from "lucide-react";
import { useState } from "react";

import { api, type Schemas, SupportTicketStatus } from "@/shared/lib/api/client";

import { CategoryPill } from "../../-components/CategoryPill";
import { CsatCard, type CsatSubmittedState } from "../../-components/CsatCard";
import { MessageBubble } from "../../-components/MessageBubble";
import { StatusPill } from "../../-components/StatusPill";

export const Route = createFileRoute("/support/tickets/$ticketId/close")({
  staticData: { trackingTitle: "Close support ticket" },
  component: CloseTicketPage
});

function CloseTicketPage() {
  const { ticketId } = Route.useParams();
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const { data: ticket, isLoading } = api.useQuery("get", "/api/account/support-tickets/{id}", {
    params: { path: { id: ticketId } }
  });

  // Local override surfaces the success state immediately on submit, before the refetch lands.
  // Server state (`ticket.csat !== null`) is the source of truth across reloads. Tracks whether the
  // user rated ("submitted") or skipped ("skipped") so the panel copy matches their action.
  const [submittedState, setSubmittedState] = useState<CsatSubmittedState>("none");

  const invalidate = () => {
    queryClient.invalidateQueries({ queryKey: ["get", "/api/account/support-tickets/{id}"] });
    queryClient.invalidateQueries({ queryKey: ["get", "/api/account/support-tickets"] });
  };

  const reopenMutation = api.useMutation("post", "/api/account/support-tickets/{id}/reopen", {
    onSuccess: () => {
      invalidate();
      navigate({ to: "/support/tickets/$ticketId", params: { ticketId } });
    }
  });

  if (isLoading) {
    return <CloseTicketSkeleton />;
  }

  if (!ticket) {
    return (
      <div className="flex flex-1 items-center justify-center text-muted-foreground">
        <Trans>Unable to load ticket.</Trans>
      </div>
    );
  }

  const tailMessages = ticket.messages.slice(-2);
  const alreadyClosed = ticket.status === SupportTicketStatus.Closed;
  // A non-null csat is treated as "already recorded" only when it's still fresh; after a reopen the
  // backend flags the existing rating as stale and the user can submit a new one.
  const csatRecorded = ticket.csat !== null && !ticket.isCsatStale;
  // Prefer the local action result; fall back to the server-recorded rating across reloads.
  const submittedDerived: CsatSubmittedState =
    submittedState !== "none" ? submittedState : csatRecorded ? "submitted" : "none";

  return (
    <div className="flex min-h-0 flex-1 flex-col">
      <div className="min-h-0 flex-1 overflow-y-auto px-4 pt-4 pb-8 sm:px-8">
        <div className="mx-auto flex w-full max-w-[48rem] flex-col">
          <div className="mb-4">
            <RouterLink
              to="/support/tickets"
              className="inline-flex items-center gap-1.5 text-xs text-muted-foreground hover:text-foreground"
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

          <div className="flex flex-col gap-5 py-2 opacity-60">
            {tailMessages.map((message) => (
              <MessageBubble key={message.id} message={message} />
            ))}
          </div>

          {alreadyClosed && (
            <div className="my-6 flex items-center justify-center">
              <span className="inline-flex items-center gap-1.5 rounded-full border border-border bg-card px-3 py-1 text-xs text-muted-foreground">
                <CheckCircle2Icon className="size-3.5" aria-hidden={true} />
                <Trans>You closed this ticket · just now</Trans>
              </span>
            </div>
          )}

          <CsatCard
            ticketId={ticketId}
            alreadyClosed={alreadyClosed}
            ticketStatus={ticket.status}
            submittedState={submittedDerived}
            csatAlreadyRecorded={csatRecorded}
            onSubmitted={(next) => {
              invalidate();
              setSubmittedState(next);
            }}
          />

          {alreadyClosed && (
            <div className="mt-4 flex items-center gap-3 rounded-lg border border-border bg-muted/40 p-4">
              <LockIcon className="size-4 text-muted-foreground" aria-hidden={true} />
              <div className="flex-1 text-sm text-muted-foreground">
                <Trans>This ticket is closed.</Trans>
              </div>
              {ticket.canBeReopened ? (
                <Button
                  variant="outline"
                  size="sm"
                  isPending={reopenMutation.isPending}
                  onClick={() =>
                    reopenMutation.mutate({ params: { path: { id: ticketId as Schemas["SupportTicketId"] } } })
                  }
                >
                  <Trans>Reopen ticket</Trans>
                </Button>
              ) : (
                <Button variant="outline" size="sm" render={<RouterLink to="/support/tickets/new" />}>
                  <Trans>New ticket</Trans>
                </Button>
              )}
            </div>
          )}
        </div>
      </div>
    </div>
  );
}

function CloseTicketSkeleton() {
  return (
    <div className="flex min-h-0 flex-1 flex-col">
      <div className="min-h-0 flex-1 overflow-hidden px-4 pt-4 pb-8 sm:px-8">
        <div className="mx-auto flex w-full max-w-[48rem] flex-col gap-4">
          <Skeleton className="h-3 w-20" />
          <Skeleton className="h-7 w-2/3" />
          <Skeleton className="h-4 w-32" />
          <Skeleton className="mt-6 h-44 w-full rounded-2xl" />
        </div>
      </div>
    </div>
  );
}
