import type { RowKey } from "@repo/ui/components/Table";

import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { Badge } from "@repo/ui/components/Badge";
import { Empty, EmptyDescription, EmptyHeader, EmptyTitle } from "@repo/ui/components/Empty";
import { Skeleton } from "@repo/ui/components/Skeleton";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@repo/ui/components/Table";
import { TenantLogo } from "@repo/ui/components/TenantLogo";
import { formatCurrency } from "@repo/utils/currency/formatCurrency";
import { Link, useNavigate } from "@tanstack/react-router";
import { ArrowRightIcon, ZapIcon } from "lucide-react";
import { useCallback } from "react";

import { SmartDateTime } from "@/shared/components/SmartDateTime";
import { api } from "@/shared/lib/api/client";
import { getBillingEventTypeLabel, getSubscriptionPlanLabel } from "@/shared/lib/api/labels";
import { BILLING_EVENT_VARIANT } from "@/shared/lib/billingEventStyle";

import { DashboardCardShell } from "./DashboardCardShell";

export function DashboardRecentStripeEventsCard() {
  const navigate = useNavigate();
  const { data, isLoading } = api.useQuery("get", "/api/back-office/dashboard/recent-stripe-events", {
    params: { query: { Limit: 6 } }
  });

  const events = data?.events ?? [];

  const handleActivate = useCallback(
    (key: RowKey) => {
      const tenantId = String(key).split("|")[0];
      navigate({ to: "/accounts/$tenantId", params: { tenantId }, search: { tab: "billing-events" } });
    },
    [navigate]
  );

  return (
    <DashboardCardShell
      title={<Trans>Recent billing events</Trans>}
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
              <Trans>No recent billing events</Trans>
            </EmptyTitle>
            <EmptyDescription>
              <Trans>Subscriptions, upgrades, and cancellations will appear here.</Trans>
            </EmptyDescription>
          </EmptyHeader>
        </Empty>
      ) : (
        <Table
          rowSize="compact"
          aria-label={t`Recent billing events`}
          selectionMode="single"
          onActivate={handleActivate}
          containerClassName="border-0 bg-transparent"
        >
          <TableHeader>
            <TableRow>
              <TableHead>
                <Trans>Account</Trans>
              </TableHead>
              <TableHead>
                <Trans>Event</Trans>
              </TableHead>
              <TableHead className="hidden md:table-cell">
                <Trans>Plan</Trans>
              </TableHead>
              <TableHead className="text-right">
                <Trans>MRR impact</Trans>
              </TableHead>
              <TableHead className="hidden text-right md:table-cell">
                <Trans>Occurred</Trans>
              </TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {events.map((event, index) => {
              const variant = BILLING_EVENT_VARIANT[event.type];
              const Icon = variant.icon;
              const isNegativeAmount = event.amountDelta != null && event.amountDelta < 0;
              return (
                <TableRow
                  key={`${event.tenantId}|${event.occurredAt}|${index}`}
                  rowKey={`${event.tenantId}|${event.occurredAt}|${index}`}
                >
                  <TableCell>
                    <div className="flex min-w-0 items-center gap-2">
                      <TenantLogo
                        logoUrl={event.tenantLogoUrl}
                        tenantName={event.tenantName}
                        size="md"
                        className="size-8 shrink-0"
                      />
                      <span className="truncate text-sm font-medium">{event.tenantName}</span>
                    </div>
                  </TableCell>
                  <TableCell>
                    <Badge variant="outline" className={`w-fit gap-1 text-xs ${variant.className}`}>
                      <Icon className="size-3" aria-hidden="true" />
                      {getBillingEventTypeLabel(event.type)}
                    </Badge>
                  </TableCell>
                  <TableCell className="hidden md:table-cell">
                    {event.toPlan != null ? (
                      <Badge variant="secondary">{getSubscriptionPlanLabel(event.toPlan)}</Badge>
                    ) : (
                      <span className="text-muted-foreground">—</span>
                    )}
                  </TableCell>
                  <TableCell
                    className={`text-right whitespace-nowrap tabular-nums ${isNegativeAmount ? "text-rose-700 dark:text-rose-300" : ""}`}
                  >
                    {event.amountDelta != null && event.currency ? (
                      formatCurrency(event.amountDelta, event.currency)
                    ) : (
                      <span className="text-muted-foreground">—</span>
                    )}
                  </TableCell>
                  <TableCell className="hidden text-right md:table-cell">
                    <SmartDateTime
                      date={event.occurredAt}
                      className="text-xs whitespace-nowrap text-muted-foreground"
                    />
                  </TableCell>
                </TableRow>
              );
            })}
          </TableBody>
        </Table>
      )}
    </DashboardCardShell>
  );
}
