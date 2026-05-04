import { Trans } from "@lingui/react/macro";
import { Badge } from "@repo/ui/components/Badge";
import { Empty, EmptyDescription, EmptyHeader, EmptyTitle } from "@repo/ui/components/Empty";
import { Skeleton } from "@repo/ui/components/Skeleton";
import { TenantLogo } from "@repo/ui/components/TenantLogo";
import { formatCurrency } from "@repo/utils/currency/formatCurrency";
import { Link } from "@tanstack/react-router";
import {
  ArrowDownRightIcon,
  ArrowRightIcon,
  ArrowUpRightIcon,
  CircleAlertIcon,
  CircleCheckIcon,
  CircleXIcon,
  ZapIcon
} from "lucide-react";

import type { StripeEventType } from "@/shared/lib/api/client";

import { SmartDateTime } from "@/shared/components/SmartDateTime";
import { api } from "@/shared/lib/api/client";
import { getStripeEventTypeLabel } from "@/shared/lib/api/labels";

import { DashboardCardShell } from "./DashboardCardShell";

const EVENT_VARIANT: Record<
  StripeEventType,
  { className: string; icon: React.ComponentType<{ className?: string; "aria-hidden"?: boolean | "true" | "false" }> }
> = {
  Subscribed: { className: "bg-emerald-500/10 text-emerald-500 border-emerald-500/20", icon: CircleCheckIcon },
  Upgraded: { className: "bg-sky-500/10 text-sky-500 border-sky-500/20", icon: ArrowUpRightIcon },
  Downgraded: { className: "bg-amber-500/10 text-amber-500 border-amber-500/20", icon: ArrowDownRightIcon },
  Canceled: { className: "bg-rose-500/10 text-rose-500 border-rose-500/20", icon: CircleXIcon },
  PaymentFailed: { className: "bg-rose-500/10 text-rose-500 border-rose-500/20", icon: CircleAlertIcon }
};

export function DashboardRecentStripeEventsCard() {
  const { data, isLoading } = api.useQuery("get", "/api/back-office/dashboard/recent-stripe-events", {
    params: { query: { Limit: 6 } }
  });

  const events = data?.events ?? [];

  return (
    <DashboardCardShell
      title={<Trans>Recent Stripe events</Trans>}
      action={
        <Link to="/accounts" className="flex items-center gap-1 text-sm text-muted-foreground hover:text-foreground">
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
            const variant = EVENT_VARIANT[event.type];
            const Icon = variant.icon;
            return (
              <li key={`${event.tenantId}-${event.occurredAt}-${index}`} className="border-b last:border-b-0">
                <Link
                  to="/accounts/$tenantId"
                  params={{ tenantId: String(event.tenantId) }}
                  className="-mx-2 flex items-center gap-3 rounded-md px-2 py-2.5 hover:bg-accent active:bg-accent"
                >
                  <TenantLogo logoUrl={event.tenantLogoUrl} tenantName={event.tenantName} size="sm" />
                  <div className="flex flex-1 flex-col">
                    <span className="text-sm font-medium">{event.tenantName}</span>
                    <span className="flex items-center gap-1.5 text-xs text-muted-foreground">
                      <Badge variant="outline" className={`gap-1 text-xs ${variant.className}`}>
                        <Icon className="size-3" aria-hidden="true" />
                        {getStripeEventTypeLabel(event.type)}
                      </Badge>
                      {event.amount !== null && event.currency && (
                        <>
                          <span aria-hidden="true">·</span>
                          <span>{formatCurrency(event.amount, event.currency)}</span>
                        </>
                      )}
                    </span>
                  </div>
                  <SmartDateTime date={event.occurredAt} className="text-xs text-muted-foreground" />
                </Link>
              </li>
            );
          })}
        </ul>
      )}
    </DashboardCardShell>
  );
}
