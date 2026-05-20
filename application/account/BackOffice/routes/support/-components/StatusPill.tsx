import { useLingui } from "@lingui/react";

import { type SupportTicketStatus } from "@/shared/lib/api/client";

import { staffStatusLabels, statusPalettes } from "./statusMaps";

export function StatusPill({ status }: { status: SupportTicketStatus }) {
  const { i18n } = useLingui();
  const palette = statusPalettes[status];
  return (
    <span
      className={`inline-flex items-center gap-1.5 rounded-full px-2 py-0.5 text-xs font-medium ring-1 ring-inset ${palette.pillClass}`}
    >
      <span className={`size-1.5 rounded-full ${palette.dotClass}`} aria-hidden={true} />
      {i18n._(staffStatusLabels[status])}
    </span>
  );
}
