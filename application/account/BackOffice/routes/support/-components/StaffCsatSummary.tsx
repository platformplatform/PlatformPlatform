import { t } from "@lingui/core/macro";
import { useFormatDate } from "@repo/ui/hooks/useSmartDate";

import { type Schemas, SupportTicketCsatScore } from "@/shared/lib/api/client";

// Mirrors the end-user CsatSummary so staff see the rating the same way the user submitted it.
// Used inside the side pane Rating section and interleaved chronologically in the chat thread on
// the detail page. The section header is provided by the caller (e.g. `<Section label={t`Rating`}>`);
// the card itself renders the smiley, label, optional comment, and the absolute submission timestamp
// so staff can place the rating relative to surrounding messages and history events.
export function StaffCsatSummary({ csat }: { csat: Schemas["TicketCsatView"] }) {
  const choice = csatDisplay[csat.score];
  const formatDate = useFormatDate();
  return (
    <div className="flex items-start gap-3 rounded-lg border border-border bg-card p-4">
      <span className="text-3xl leading-none" aria-hidden={true}>
        {choice.emoji}
      </span>
      <div className="flex min-w-0 flex-1 flex-col gap-1">
        <span className="text-sm font-medium">{choice.label()}</span>
        {csat.comment && <p className="text-sm text-muted-foreground">{csat.comment}</p>}
        <span className="text-xs text-muted-foreground tabular-nums">{formatDate(csat.submittedAt, true, true)}</span>
      </div>
    </div>
  );
}

// Compact variant rendered below the status pill in the inbox row — smiley plus label so the
// rating is scannable in a horizontal sweep of the table.
export function CsatScoreLabel({ score }: { score: Schemas["SupportTicketCsatScore"] }) {
  const choice = csatDisplay[score];
  return (
    <span className="inline-flex items-center gap-1 text-xs text-muted-foreground">
      <span aria-hidden={true}>{choice.emoji}</span>
      <span>{choice.label()}</span>
    </span>
  );
}

const csatDisplay: Record<Schemas["SupportTicketCsatScore"], { emoji: string; label: () => string }> = {
  [SupportTicketCsatScore.Helpful]: { emoji: "😊", label: () => t`Helpful` },
  [SupportTicketCsatScore.Ok]: { emoji: "😐", label: () => t`OK` },
  [SupportTicketCsatScore.NotGreat]: { emoji: "😠", label: () => t`Not great` }
};
