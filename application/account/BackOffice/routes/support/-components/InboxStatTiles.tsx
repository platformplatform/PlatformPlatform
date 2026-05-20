import type { MessageDescriptor } from "@lingui/core";

import { msg } from "@lingui/core/macro";
import { useLingui } from "@lingui/react";
import { ToggleGroup, ToggleGroupItem } from "@repo/ui/components/ToggleGroup";

import type { Schemas } from "@/shared/lib/api/client";

import { SupportTicketStatus } from "@/shared/lib/api/client";

import { statusPalettes } from "./statusMaps";

interface TileDefinition {
  key: SupportTicketStatus;
  label: MessageDescriptor;
  countKey: keyof Schemas["AllTicketsCounts"];
}

const TILES: TileDefinition[] = [
  { key: SupportTicketStatus.New, label: msg`New`, countKey: "new" },
  { key: SupportTicketStatus.AwaitingAgent, label: msg`Awaiting agent`, countKey: "awaitingAgent" },
  { key: SupportTicketStatus.AwaitingUser, label: msg`Awaiting user`, countKey: "awaitingUser" },
  { key: SupportTicketStatus.AwaitingInternal, label: msg`Awaiting internal`, countKey: "awaitingInternal" },
  { key: SupportTicketStatus.Resolved, label: msg`Resolved (24h)`, countKey: "resolvedLast24Hours" }
];

interface InboxStatTilesProps {
  counts: Schemas["AllTicketsCounts"] | undefined;
  selectedStatus: SupportTicketStatus | undefined;
  onSelect: (status: SupportTicketStatus | undefined) => void;
}

export function InboxStatTiles({ counts, selectedStatus, onSelect }: Readonly<InboxStatTilesProps>) {
  const { i18n } = useLingui();
  return (
    <ToggleGroup
      spacing={3}
      aria-label={i18n._(msg`Filter by status`)}
      value={selectedStatus ? [selectedStatus] : []}
      onValueChange={(values) => onSelect((values[values.length - 1] as SupportTicketStatus) ?? undefined)}
      className="grid w-full grid-cols-2 sm:grid-cols-3 lg:grid-cols-5"
    >
      {TILES.map((tile) => (
        <ToggleGroupItem
          key={tile.key}
          value={tile.key}
          // No baseline ring at rest — only the selected tile renders a thick ring (handled by
          // each palette's `aria-pressed:ring-2 aria-pressed:ring-<color>` classes) so unselected
          // chips read as flat tinted tiles, matching the prototype.
          className={`flex !h-auto !min-w-0 flex-col items-start gap-1 rounded-lg px-4 py-3 text-left transition-colors ring-inset active:opacity-80 ${statusPalettes[tile.key].tileClass}`}
        >
          <span className="text-xs font-medium opacity-80">{i18n._(tile.label)}</span>
          <span className="text-2xl font-semibold tabular-nums">{counts?.[tile.countKey] ?? 0}</span>
        </ToggleGroupItem>
      ))}
    </ToggleGroup>
  );
}
