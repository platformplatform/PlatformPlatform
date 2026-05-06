import { Trans } from "@lingui/react/macro";
import { Badge } from "@repo/ui/components/Badge";
import { Empty, EmptyDescription, EmptyHeader, EmptyTitle } from "@repo/ui/components/Empty";
import { Skeleton } from "@repo/ui/components/Skeleton";
import { formatCurrency } from "@repo/utils/currency/formatCurrency";

import type { components } from "@/shared/lib/api/client";

import { SmartDateTime } from "@/shared/components/SmartDateTime";
import { getBillingEventTypeLabel, getSubscriptionPlanLabel } from "@/shared/lib/api/labels";
import { BILLING_EVENT_VARIANT } from "@/shared/lib/billingEventStyle";

type BillingEventSummary = components["schemas"]["BillingEventSummary"];

interface Props {
  events: BillingEventSummary[];
  isLoading: boolean;
  /** Limit how many events to show. Used by the Overview tab to render a condensed summary. */
  maxItems?: number;
}

export function AccountBillingHistorySection({ events, isLoading, maxItems }: Readonly<Props>) {
  const visibleEvents = maxItems != null ? events.slice(0, maxItems) : events;

  if (isLoading && events.length === 0) {
    return (
      <div className="flex flex-col gap-2 rounded-lg border border-border bg-card p-2">
        {Array.from({ length: maxItems ?? 4 }).map((_, index) => (
          <Skeleton key={`event-skeleton-${index}`} className="h-12 w-full" />
        ))}
      </div>
    );
  }

  if (visibleEvents.length === 0) {
    return (
      <Empty className="border bg-card">
        <EmptyHeader>
          <EmptyTitle>
            <Trans>No billing events</Trans>
          </EmptyTitle>
          <EmptyDescription>
            <Trans>Subscription, payment, and billing transitions will appear here.</Trans>
          </EmptyDescription>
        </EmptyHeader>
      </Empty>
    );
  }

  return (
    <ul className="flex flex-col rounded-lg border border-border bg-card">
      {visibleEvents.map((event) => {
        const variant = BILLING_EVENT_VARIANT[event.eventType];
        const Icon = variant.icon;
        const showPlanTransition = event.fromPlan != null && event.toPlan != null && event.fromPlan !== event.toPlan;
        const isNegativeAmount = event.amountDelta != null && event.amountDelta < 0;
        return (
          <li
            key={event.id}
            className="grid grid-cols-[minmax(0,12rem)_minmax(0,1fr)_auto_auto] items-center gap-3 border-b px-3 py-2.5 last:border-b-0"
          >
            <Badge variant="outline" className={`w-fit gap-1 text-xs ${variant.className}`}>
              <Icon className="size-3" aria-hidden={true} />
              {getBillingEventTypeLabel(event.eventType)}
            </Badge>
            {showPlanTransition ? (
              <span className="text-xs text-muted-foreground">
                {getSubscriptionPlanLabel(event.fromPlan!)} → {getSubscriptionPlanLabel(event.toPlan!)}
              </span>
            ) : (
              <span aria-hidden={true} />
            )}
            <span
              className={`text-right text-sm whitespace-nowrap tabular-nums ${
                isNegativeAmount ? "text-rose-500" : "text-muted-foreground"
              }`}
            >
              {event.amountDelta != null && event.currency ? formatCurrency(event.amountDelta, event.currency) : ""}
            </span>
            <SmartDateTime date={event.occurredAt} className="text-xs whitespace-nowrap text-muted-foreground" />
          </li>
        );
      })}
    </ul>
  );
}
