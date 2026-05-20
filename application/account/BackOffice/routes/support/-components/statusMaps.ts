import type { MessageDescriptor } from "@lingui/core";

import { msg } from "@lingui/core/macro";

import { SupportTicketCategory, SupportTicketStatus } from "@/shared/lib/api/client";

export const staffStatusLabels: Record<SupportTicketStatus, MessageDescriptor> = {
  [SupportTicketStatus.New]: msg`New`,
  [SupportTicketStatus.AwaitingAgent]: msg`Awaiting agent`,
  [SupportTicketStatus.AwaitingUser]: msg`Awaiting user`,
  [SupportTicketStatus.AwaitingInternal]: msg`Awaiting internal`,
  [SupportTicketStatus.Resolved]: msg`Resolved`,
  [SupportTicketStatus.Closed]: msg`Closed`
};

type StatusPalette = {
  pillClass: string;
  dotClass: string;
  tileClass: string;
};

// Active states carry meaningful colors. AwaitingUser alone stays neutral grey, since it is the
// most common state for active tickets and shouting at the user would feel adversarial. Tiles
// render WITHOUT a ring at rest; the selected tile gets a thick (ring-2) ring in the tile's own
// colour and must explicitly preserve its tint background. Otherwise ShadCN ToggleGroup's default
// `data-[state=on]:bg-primary` paints the active chip solid black AND Toggle's `hover:bg-muted`
// turns the chip grey on hover. Both overrides are per-palette via tw-merge.
export const statusPalettes: Record<SupportTicketStatus, StatusPalette> = {
  [SupportTicketStatus.New]: {
    pillClass: "bg-info/10 text-info ring-info/25",
    dotClass: "bg-info",
    tileClass:
      "bg-info/10 text-info hover:bg-info/15 hover:text-info aria-pressed:bg-info/10 aria-pressed:text-info aria-pressed:ring-2 aria-pressed:ring-info data-[state=on]:bg-info/10 data-[state=on]:text-info"
  },
  [SupportTicketStatus.AwaitingAgent]: {
    pillClass: "bg-warning/15 text-warning ring-warning/30",
    dotClass: "bg-warning",
    tileClass:
      "bg-warning/10 text-warning hover:bg-warning/15 hover:text-warning aria-pressed:bg-warning/10 aria-pressed:text-warning aria-pressed:ring-2 aria-pressed:ring-warning data-[state=on]:bg-warning/10 data-[state=on]:text-warning"
  },
  [SupportTicketStatus.AwaitingUser]: {
    pillClass: "bg-muted text-muted-foreground ring-border",
    dotClass: "bg-muted-foreground",
    tileClass:
      "bg-muted text-muted-foreground hover:bg-muted/80 hover:text-muted-foreground aria-pressed:bg-muted aria-pressed:text-muted-foreground aria-pressed:ring-2 aria-pressed:ring-muted-foreground data-[state=on]:bg-muted data-[state=on]:text-muted-foreground"
  },
  [SupportTicketStatus.AwaitingInternal]: {
    pillClass: "bg-purple-500/10 text-purple-700 ring-purple-500/25 dark:text-purple-300",
    dotClass: "bg-purple-500",
    tileClass:
      "bg-purple-500/10 text-purple-700 hover:bg-purple-500/15 hover:text-purple-700 aria-pressed:bg-purple-500/10 aria-pressed:text-purple-700 aria-pressed:ring-2 aria-pressed:ring-purple-500 data-[state=on]:bg-purple-500/10 data-[state=on]:text-purple-700 dark:text-purple-300 dark:hover:text-purple-300 dark:aria-pressed:text-purple-300 dark:data-[state=on]:text-purple-300"
  },
  [SupportTicketStatus.Resolved]: {
    pillClass: "bg-success/10 text-success ring-success/25",
    dotClass: "bg-success",
    tileClass:
      "bg-success/10 text-success hover:bg-success/15 hover:text-success aria-pressed:bg-success/10 aria-pressed:text-success aria-pressed:ring-2 aria-pressed:ring-success data-[state=on]:bg-success/10 data-[state=on]:text-success"
  },
  [SupportTicketStatus.Closed]: {
    pillClass: "bg-muted text-muted-foreground ring-border",
    dotClass: "bg-muted-foreground",
    tileClass:
      "bg-muted text-muted-foreground hover:bg-muted/80 hover:text-muted-foreground aria-pressed:bg-muted aria-pressed:text-muted-foreground aria-pressed:ring-2 aria-pressed:ring-muted-foreground data-[state=on]:bg-muted data-[state=on]:text-muted-foreground"
  }
};

export const categoryLabels: Record<SupportTicketCategory, MessageDescriptor> = {
  [SupportTicketCategory.Billing]: msg`Billing`,
  [SupportTicketCategory.Account]: msg`Account`,
  [SupportTicketCategory.HowTo]: msg`How-to`,
  [SupportTicketCategory.Bug]: msg`Bug`,
  [SupportTicketCategory.Feature]: msg`Feature`,
  [SupportTicketCategory.Feedback]: msg`Feedback`,
  [SupportTicketCategory.Other]: msg`Other`
};

// Category is the single most important badge in the inbox; staff scan by topic. Each gets a
// distinct hue so the eye can pick out e.g. all the "Bug" tickets at a glance. Other stays neutral.
export const categoryPaletteClasses: Record<SupportTicketCategory, string> = {
  [SupportTicketCategory.Billing]: "bg-emerald-500/10 text-emerald-700 dark:text-emerald-300",
  [SupportTicketCategory.Account]: "bg-blue-500/10 text-blue-700 dark:text-blue-300",
  [SupportTicketCategory.HowTo]: "bg-cyan-500/10 text-cyan-700 dark:text-cyan-300",
  [SupportTicketCategory.Bug]: "bg-rose-500/10 text-rose-700 dark:text-rose-300",
  [SupportTicketCategory.Feature]: "bg-fuchsia-500/10 text-fuchsia-700 dark:text-fuchsia-300",
  [SupportTicketCategory.Feedback]: "bg-amber-500/10 text-amber-700 dark:text-amber-300",
  [SupportTicketCategory.Other]: "bg-muted text-muted-foreground"
};
