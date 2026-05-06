import { Trans } from "@lingui/react/macro";
import { Badge } from "@repo/ui/components/Badge";
import { Empty, EmptyDescription, EmptyHeader, EmptyTitle } from "@repo/ui/components/Empty";
import { Skeleton } from "@repo/ui/components/Skeleton";
import { TenantLogo } from "@repo/ui/components/TenantLogo";
import { formatCurrency } from "@repo/utils/currency/formatCurrency";
import { Link } from "@tanstack/react-router";
import { ArrowRightIcon, ZapIcon } from "lucide-react";

import { SmartDateTime } from "@/shared/components/SmartDateTime";
import { api, BillingEventType } from "@/shared/lib/api/client";
import { getBillingEventTypeLabel, getSubscriptionPlanLabel } from "@/shared/lib/api/labels";
import { BILLING_EVENT_VARIANT } from "@/shared/lib/billingEventStyle";

import { DashboardCardShell } from "./DashboardCardShell";

export function DashboardRecentStripeEventsCard() {
  const { data, isLoading } = api.useQuery("get", "/api/back-office/dashboard/recent-stripe-events", {
    params: { query: { Limit: 6 } }
  });

  // Filter out low-signal billing-info events from the dashboard card; they are still visible on the
  // full /billing-events page for operators who need to audit them.
  const events = (data?.events ?? []).filter((event) => event.type !== BillingEventType.BillingInfoAdded);

  return (
    <DashboardCardShell
      title={<Trans>Recent Stripe events</Trans>}
      action={
        <Link
          to="/billing-events"
          className="flex items-center gap-1 text-sm text-muted-foreground hover:text-foreground"
        >
          <Trans>View all</Trans>
          <ArrowRightIcon className="size-3.5" aria-hidden="true" />
        </Link>
      }
    >
      {isLoading ? (
        <div className="flex flex-col gap-3">
          {[0, 1, 2, 3, 4, 5].map((index) => (
            <Skeleton key={index} className="h-12 w-full" />
          ))}
        </div>
      ) : events.length === 0 ? (
        <Empty className="border bg-card">
          <EmptyHeader>
            <ZapIcon className="size-6 text-muted-foreground" aria-hidden="true" />
            <EmptyTitle>
              <Trans>No recent Stripe events</Trans>
            </EmptyTitle>
            <EmptyDescription>
              <Trans>Subscriptions, upgrades, and cancellations will appear here.</Trans>
            </EmptyDescription>
          </EmptyHeader>
        </Empty>
      ) : (
        <ul className="flex flex-col">
          {events.map((event, index) => {
            const variant = BILLING_EVENT_VARIANT[event.type];
            const Icon = variant.icon;
            const showPlanTransition =
              event.fromPlan != null && event.toPlan != null && event.fromPlan !== event.toPlan;
            const isNegativeAmount = event.amountDelta != null && event.amountDelta < 0;
            return (
              <li key={`${event.tenantId}-${event.occurredAt}-${index}`} className="border-b last:border-b-0">
                <Link
                  to="/accounts/$tenantId"
                  params={{ tenantId: String(event.tenantId) }}
                  search={{ tab: "billing" }}
                  className="-mx-2 grid grid-cols-[auto_minmax(0,9rem)_auto_minmax(0,1fr)_auto_auto] items-center gap-3 rounded-md px-2 py-2.5 hover:bg-accent active:bg-accent"
                >
                  <TenantLogo
                    logoUrl={event.tenantLogoUrl}
                    tenantName={event.tenantName}
                    size="md"
                    className="size-10"
                  />
                  <span className="truncate text-sm font-medium">{event.tenantName}</span>
                  <Badge variant="outline" className={`w-fit gap-1 text-xs ${variant.className}`}>
                    <Icon className="size-3" aria-hidden="true" />
                    {getBillingEventTypeLabel(event.type)}
                  </Badge>
                  {showPlanTransition ? (
                    <span className="truncate text-xs text-muted-foreground">
                      {getSubscriptionPlanLabel(event.fromPlan!)} → {getSubscriptionPlanLabel(event.toPlan!)}
                    </span>
                  ) : (
                    <span aria-hidden="true" />
                  )}
                  <span
                    className={`text-right text-sm whitespace-nowrap tabular-nums ${
                      isNegativeAmount ? "text-rose-500" : "text-muted-foreground"
                    }`}
                  >
                    {event.amountDelta != null && event.currency
                      ? formatCurrency(event.amountDelta, event.currency)
                      : ""}
                  </span>
                  <SmartDateTime date={event.occurredAt} className="text-xs whitespace-nowrap text-muted-foreground" />
                </Link>
              </li>
            );
          })}
        </ul>
      )}
    </DashboardCardShell>
  );
}
