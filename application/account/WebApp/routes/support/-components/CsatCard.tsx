import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { Button } from "@repo/ui/components/Button";
import { Label } from "@repo/ui/components/Label";
import { Textarea } from "@repo/ui/components/Textarea";
import { Link as RouterLink } from "@tanstack/react-router";
import { useId, useState } from "react";

import { api, type Schemas, SupportTicketCsatScore, SupportTicketStatus } from "@/shared/lib/api/client";

import { CsatSmileyButton } from "./CsatSmileyButton";

export type CsatSubmittedState = "none" | "submitted" | "skipped";

type CsatChoice = {
  score: Schemas["SupportTicketCsatScore"];
  emoji: string;
  label: () => string;
  placeholder: () => string;
};

const csatChoices: CsatChoice[] = [
  {
    score: SupportTicketCsatScore.Helpful,
    emoji: "😊",
    label: () => t`Helpful`,
    placeholder: () => t`What worked well?`
  },
  {
    score: SupportTicketCsatScore.Ok,
    emoji: "😐",
    label: () => t`OK`,
    placeholder: () => t`What could have been better?`
  },
  {
    score: SupportTicketCsatScore.NotGreat,
    emoji: "😠",
    label: () => t`Not great`,
    placeholder: () => t`What went wrong? We'll read every word.`
  }
];

interface CsatCardProps {
  ticketId: string;
  alreadyClosed: boolean;
  // The ticket's current status. Used to decide whether "Skip" still needs a /close call: when the
  // ticket is already terminal (e.g. the user arrived here via a mark-as-resolved reply) /close would
  // 400, so Skip becomes a no-op network-wise.
  ticketStatus: Schemas["SupportTicketStatus"];
  submittedState: CsatSubmittedState;
  csatAlreadyRecorded: boolean;
  onSubmitted: (next: Exclude<CsatSubmittedState, "none">) => void;
}

export function CsatCard({
  ticketId,
  alreadyClosed,
  ticketStatus,
  submittedState,
  csatAlreadyRecorded,
  onSubmitted
}: Readonly<CsatCardProps>) {
  const commentId = useId();
  const [score, setScore] = useState<Schemas["SupportTicketCsatScore"]>(SupportTicketCsatScore.Helpful);
  const [comment, setComment] = useState("");

  const closeMutation = api.useMutation("post", "/api/account/support-tickets/{id}/close");
  const csatMutation = api.useMutation("post", "/api/account/support-tickets/{id}/csat", {
    onSuccess: () => onSubmitted("submitted")
  });

  const isPending = closeMutation.isPending || csatMutation.isPending;
  const trimmedComment = comment.trim() || null;
  const isTerminalAlready =
    ticketStatus === SupportTicketStatus.Resolved || ticketStatus === SupportTicketStatus.Closed;

  const handleSkip = () => {
    // Skip means "don't leave a rating". When the ticket is already terminal there is nothing to
    // transition — calling /close would 400 ("already resolved or closed"). Treat it as a no-op.
    if (isTerminalAlready) {
      onSubmitted("skipped");
      return;
    }
    closeMutation.mutate(
      {
        params: { path: { id: ticketId as Schemas["SupportTicketId"] } },
        body: { csatScore: null, csatComment: null }
      },
      { onSuccess: () => onSubmitted("skipped") }
    );
  };

  const handleSubmit = () => {
    if (alreadyClosed) {
      csatMutation.mutate({
        params: { path: { id: ticketId as Schemas["SupportTicketId"] } },
        body: { score, comment: trimmedComment }
      });
      return;
    }
    closeMutation.mutate(
      {
        params: { path: { id: ticketId as Schemas["SupportTicketId"] } },
        body: { csatScore: score, csatComment: trimmedComment }
      },
      { onSuccess: () => onSubmitted("submitted") }
    );
  };

  if (submittedState !== "none") {
    return (
      <div className="rounded-2xl border border-border bg-card p-6 text-center">
        <h2 className="mb-1">
          {submittedState === "submitted" ? <Trans>Thanks for the feedback</Trans> : <Trans>Ticket closed</Trans>}
        </h2>
        <p className="mb-4 text-sm text-muted-foreground">
          {submittedState === "submitted" ? (
            <Trans>It helps us spot what to improve.</Trans>
          ) : (
            <Trans>You can reopen it from your tickets list if you change your mind.</Trans>
          )}
        </p>
        <Button variant="outline" size="sm" render={<RouterLink to="/support/tickets" />}>
          <Trans>Back to My tickets</Trans>
        </Button>
      </div>
    );
  }

  // CSAT is one-shot — if it's already recorded server-side (e.g. user revisited the URL or
  // submitted on an earlier visit), the form would let them overwrite the score silently. Show a
  // read-only "already submitted" state instead.
  if (csatAlreadyRecorded) {
    return (
      <div className="rounded-2xl border border-border bg-card p-6 text-center">
        <h2 className="mb-1">
          <Trans>You already shared feedback for this ticket</Trans>
        </h2>
        <p className="mb-4 text-sm text-muted-foreground">
          <Trans>Thanks — we won't ask again.</Trans>
        </p>
        <Button variant="outline" size="sm" render={<RouterLink to="/support/tickets" />}>
          <Trans>Back to My tickets</Trans>
        </Button>
      </div>
    );
  }

  return (
    <div className="rounded-2xl border border-border bg-card p-6 text-center">
      <h2 className="mb-1">
        <Trans>Thanks for closing this out</Trans>
      </h2>
      <p className="mb-5 text-sm text-muted-foreground">
        <Trans>One quick question — how was the support you got?</Trans>
      </p>
      <div className="flex items-center justify-center gap-4">
        {csatChoices.map((choice) => (
          <CsatSmileyButton
            key={choice.score}
            emoji={choice.emoji}
            label={choice.label()}
            selected={score === choice.score}
            disabled={isPending}
            onSelect={() => setScore(choice.score)}
          />
        ))}
      </div>
      <div className="mt-5 text-left">
        <Label htmlFor={commentId} className="mb-1.5 text-xs font-normal text-muted-foreground">
          <Trans>Anything you want to add?</Trans>
          <span className="opacity-70">
            <Trans>(optional)</Trans>
          </span>
        </Label>
        <Textarea
          id={commentId}
          rows={3}
          placeholder={csatChoices.find((choice) => choice.score === score)?.placeholder() ?? ""}
          value={comment}
          onChange={(event) => setComment(event.target.value)}
          disabled={isPending}
        />
      </div>
      <div className="mt-5 flex items-center justify-center gap-2">
        <Button variant="ghost" size="sm" disabled={isPending} onClick={handleSkip}>
          <Trans>Skip</Trans>
        </Button>
        <Button size="sm" isPending={isPending} onClick={handleSubmit}>
          {isPending ? <Trans>Submitting...</Trans> : <Trans>Submit feedback</Trans>}
        </Button>
      </div>
    </div>
  );
}
