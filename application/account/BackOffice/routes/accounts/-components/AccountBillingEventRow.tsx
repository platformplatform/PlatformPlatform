import type { ReactNode } from "react";

import { Trans } from "@lingui/react/macro";
import { Badge } from "@repo/ui/components/Badge";
import { TableCell, TableRow } from "@repo/ui/components/Table";
import { formatCurrency } from "@repo/utils/currency/formatCurrency";

import type { components } from "@/shared/lib/api/client";

import { getBillingEventTypeLabel, getSubscriptionPlanLabel } from "@/shared/lib/api/labels";
import { BILLING_EVENT_VARIANT } from "@/shared/lib/billingEventStyle";

type BillingEventSummary = components["schemas"]["BillingEventSummary"];

function isSameDay(a: string, b: string): boolean {
  return a.slice(0, 10) === b.slice(0, 10);
}

export function AccountBillingEventRow({
  event,
  renderDate,
  isCompact
}: Readonly<{
  event: BillingEventSummary;
  renderDate: (value: string | null | undefined) => ReactNode;
  isCompact: boolean;
}>) {
  const variant = BILLING_EVENT_VARIANT[event.eventType];
  const Icon = variant.icon;
  const showPlanTransition = event.fromPlan != null && event.toPlan != null && event.fromPlan !== event.toPlan;
  const showEffective = event.effectiveAt != null && !isSameDay(event.effectiveAt, event.occurredAt);
  return (
    <TableRow rowKey={event.id}>
      <TableCell className="align-top whitespace-nowrap">
        <div className="flex flex-col leading-tight">
          <span>{renderDate(event.occurredAt)}</span>
          {showEffective && (
            <span className="text-xs text-muted-foreground">
              <Trans>Effective {renderDate(event.effectiveAt)}</Trans>
            </span>
          )}
        </div>
      </TableCell>
      <TableCell>
        <Badge variant="outline" className={`gap-1 text-xs ${variant.className}`}>
          <Icon className="size-3" aria-hidden={true} />
          {getBillingEventTypeLabel(event.eventType)}
        </Badge>
      </TableCell>
      <TableCell className="hidden md:table-cell">
        {showPlanTransition ? (
          <span className="inline-flex items-center gap-1 whitespace-nowrap">
            <Badge variant="secondary">{getSubscriptionPlanLabel(event.fromPlan!)}</Badge>
            <span aria-hidden={true} className="text-muted-foreground">
              →
            </span>
            <Badge variant="secondary">{getSubscriptionPlanLabel(event.toPlan!)}</Badge>
          </span>
        ) : event.toPlan != null ? (
          <Badge variant="secondary">{getSubscriptionPlanLabel(event.toPlan)}</Badge>
        ) : (
          <span className="text-muted-foreground">—</span>
        )}
      </TableCell>
      {isCompact ? <CompactAmountCell event={event} /> : <MrrImpactAndAfterCells event={event} />}
    </TableRow>
  );
}

function CompactAmountCell({ event }: Readonly<{ event: BillingEventSummary }>) {
  const isNegativeAmount = event.amountDelta != null && event.amountDelta < 0;
  return (
    <TableCell className={`text-right whitespace-nowrap tabular-nums ${isNegativeAmount ? "text-rose-500" : ""}`}>
      {event.amountDelta != null && event.currency ? (
        formatCurrency(event.amountDelta, event.currency)
      ) : (
        <span className="text-muted-foreground">—</span>
      )}
    </TableCell>
  );
}

function MrrImpactAndAfterCells({ event }: Readonly<{ event: BillingEventSummary }>) {
  const isNegativeAmount = event.amountDelta != null && event.amountDelta < 0;
  return (
    <>
      <TableCell className={`text-right whitespace-nowrap tabular-nums ${isNegativeAmount ? "text-rose-500" : ""}`}>
        {event.amountDelta != null && event.currency ? formatCurrency(event.amountDelta, event.currency) : "—"}
      </TableCell>
      <TableCell className="hidden text-right whitespace-nowrap text-muted-foreground tabular-nums md:table-cell">
        {event.newAmount != null && event.currency ? formatCurrency(event.newAmount, event.currency) : "—"}
      </TableCell>
    </>
  );
}
