import type { MessageDescriptor } from "@lingui/core";

import { msg } from "@lingui/core/macro";

import { SupportTicketCategory, SupportTicketStatus } from "@/shared/lib/api/client";

export const userStatusLabels: Record<SupportTicketStatus, MessageDescriptor> = {
  [SupportTicketStatus.New]: msg`Sent`,
  [SupportTicketStatus.AwaitingAgent]: msg`Waiting on support`,
  [SupportTicketStatus.AwaitingUser]: msg`Awaiting your reply`,
  [SupportTicketStatus.AwaitingInternal]: msg`Looking into it`,
  [SupportTicketStatus.Resolved]: msg`Marked as solved`,
  [SupportTicketStatus.Closed]: msg`Closed`
};

type StatusPalette = {
  pillClass: string;
  dotClass: string;
};

// Mirrors the back-office StatusPill palette so the same status reads consistently to the user and
// to staff. New / AwaitingAgent / Resolved are meaningful and colored; the other three are neutral.
export const statusPalettes: Record<SupportTicketStatus, StatusPalette> = {
  [SupportTicketStatus.New]: {
    pillClass: "bg-info/10 text-info ring-info/25",
    dotClass: "bg-info"
  },
  [SupportTicketStatus.AwaitingAgent]: {
    pillClass: "bg-warning/15 text-warning ring-warning/30",
    dotClass: "bg-warning"
  },
  [SupportTicketStatus.AwaitingUser]: {
    pillClass: "bg-muted text-muted-foreground ring-border",
    dotClass: "bg-muted-foreground"
  },
  [SupportTicketStatus.AwaitingInternal]: {
    pillClass: "bg-muted text-muted-foreground ring-border",
    dotClass: "bg-muted-foreground"
  },
  [SupportTicketStatus.Resolved]: {
    pillClass: "bg-success/10 text-success ring-success/25",
    dotClass: "bg-success"
  },
  [SupportTicketStatus.Closed]: {
    pillClass: "bg-muted text-muted-foreground ring-border",
    dotClass: "bg-muted-foreground"
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
