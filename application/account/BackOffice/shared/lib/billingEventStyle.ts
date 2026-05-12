import {
  ArrowDownRightIcon,
  ArrowUpRightIcon,
  CalendarClockIcon,
  CircleAlertIcon,
  CircleCheckIcon,
  CircleSlashIcon,
  CircleXIcon,
  CreditCardIcon,
  PauseCircleIcon,
  RefreshCwIcon,
  ReplyIcon,
  RotateCcwIcon,
  TriangleAlertIcon,
  WalletIcon
} from "lucide-react";

import { BillingEventType } from "@/shared/lib/api/client";

export interface BillingEventVariant {
  className: string;
  icon: React.ComponentType<{ className?: string; "aria-hidden"?: boolean | "true" | "false" }>;
}

/**
 * Centralised badge styling for the BillingEventType enum. Used by the dashboard "Recent billing events"
 * card and the /billing-events table so the colour and icon are consistent everywhere a billing event is
 * surfaced.
 */
export const BILLING_EVENT_VARIANT: Record<BillingEventType, BillingEventVariant> = {
  [BillingEventType.SubscriptionCreated]: {
    className: "bg-emerald-500/10 text-emerald-700 border-emerald-500/20 dark:text-emerald-300",
    icon: CircleCheckIcon
  },
  [BillingEventType.SubscriptionRenewed]: {
    className: "bg-emerald-500/10 text-emerald-700 border-emerald-500/20 dark:text-emerald-300",
    icon: RefreshCwIcon
  },
  [BillingEventType.SubscriptionUpgraded]: {
    className: "bg-emerald-500/10 text-emerald-700 border-emerald-500/20 dark:text-emerald-300",
    icon: ArrowUpRightIcon
  },
  [BillingEventType.SubscriptionDowngradeScheduled]: {
    className: "bg-amber-500/10 text-amber-700 border-amber-500/20 dark:text-amber-300",
    icon: CalendarClockIcon
  },
  [BillingEventType.SubscriptionDowngradeCancelled]: {
    className: "bg-emerald-500/10 text-emerald-700 border-emerald-500/20 dark:text-emerald-300",
    icon: RotateCcwIcon
  },
  [BillingEventType.SubscriptionDowngraded]: {
    className: "bg-amber-500/10 text-amber-700 border-amber-500/20 dark:text-amber-300",
    icon: ArrowDownRightIcon
  },
  [BillingEventType.SubscriptionCancelled]: {
    className: "bg-rose-500/10 text-rose-700 border-rose-500/20 dark:text-rose-300",
    icon: CircleXIcon
  },
  [BillingEventType.SubscriptionReactivated]: {
    className: "bg-emerald-500/10 text-emerald-700 border-emerald-500/20 dark:text-emerald-300",
    icon: ReplyIcon
  },
  [BillingEventType.SubscriptionExpired]: {
    className: "bg-rose-500/10 text-rose-700 border-rose-500/20 dark:text-rose-300",
    icon: CircleXIcon
  },
  [BillingEventType.SubscriptionImmediatelyCancelled]: {
    className: "bg-rose-500/10 text-rose-700 border-rose-500/20 dark:text-rose-300",
    icon: CircleXIcon
  },
  [BillingEventType.SubscriptionSuspended]: {
    className: "bg-rose-500/10 text-rose-700 border-rose-500/20 dark:text-rose-300",
    icon: PauseCircleIcon
  },
  [BillingEventType.SubscriptionPastDue]: {
    className: "bg-amber-500/10 text-amber-700 border-amber-500/30 dark:text-amber-300",
    icon: CircleAlertIcon
  },
  [BillingEventType.PaymentFailed]: {
    className: "bg-rose-500/10 text-rose-700 border-rose-500/20 dark:text-rose-300",
    icon: CircleAlertIcon
  },
  [BillingEventType.PaymentRecovered]: {
    className: "bg-emerald-500/10 text-emerald-700 border-emerald-500/20 dark:text-emerald-300",
    icon: CircleCheckIcon
  },
  [BillingEventType.PaymentRefunded]: {
    className: "bg-amber-500/10 text-amber-700 border-amber-500/20 dark:text-amber-300",
    icon: ArrowDownRightIcon
  },
  [BillingEventType.BillingInfoAdded]: {
    className: "bg-sky-500/10 text-sky-700 border-sky-500/20 dark:text-sky-300",
    icon: WalletIcon
  },
  [BillingEventType.BillingInfoUpdated]: {
    className: "bg-sky-500/10 text-sky-700 border-sky-500/20 dark:text-sky-300",
    icon: WalletIcon
  },
  [BillingEventType.PaymentMethodUpdated]: {
    className: "bg-sky-500/10 text-sky-700 border-sky-500/20 dark:text-sky-300",
    icon: CreditCardIcon
  },
  [BillingEventType.NoOp]: {
    className: "bg-muted text-muted-foreground border-border",
    icon: CircleSlashIcon
  },
  [BillingEventType.Unclassified]: {
    className: "bg-amber-500/10 text-amber-700 border-amber-500/30 dark:text-amber-300",
    icon: TriangleAlertIcon
  }
};
