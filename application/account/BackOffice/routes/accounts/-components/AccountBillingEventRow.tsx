import type { ReactNode } from "react";

import { Badge } from "@repo/ui/components/Badge";
import { TableCell, TableRow } from "@repo/ui/components/Table";
import { formatCurrency } from "@repo/utils/currency/formatCurrency";

import type { components } from "@/shared/lib/api/client";

import { getBillingEventTypeLabel, getSubscriptionPlanLabel } from "@/shared/lib/api/labels";
import { PLAN_TRANSITION_EVENT_TYPES } from "@/shared/lib/billingEventCategories";
import { getDisplayedPlanTransition } from "@/shared/lib/billingEventPlanTransition";
import { BILLING_EVENT_VARIANT } from "@/shared/lib/billingEventStyle";

type BillingEventSummary = components["schemas"]["BillingEventSummary"];

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
  const planTransition = PLAN_TRANSITION_EVENT_TYPES.has(event.eventType)
    ? getDisplayedPlanTransition(event.eventType, event.fromPlan, event.toPlan)
    : null;
  return (
    <TableRow rowKey={event.id}>
      <TableCell className="align-top whitespace-nowrap">
        <div className="flex flex-col leading-tight">
          <span>{renderDate(event.occurredAt)}</span>
        </div>
      </TableCell>
      <TableCell>
        <Badge variant="outline" className={`gap-1 text-xs ${variant.className}`}>
          <Icon className="size-3" aria-hidden={true} />
          {getBillingEventTypeLabel(event.eventType)}
        </Badge>
      </TableCell>
      <TableCell className="hidden md:table-cell">
        {planTransition != null ? (
          <span className="inline-flex items-center gap-1 whitespace-nowrap">
            <Badge variant="secondary">{getSubscriptionPlanLabel(planTransition.from)}</Badge>
            <span aria-hidden={true} className="text-muted-foreground">
              →
            </span>
            <Badge variant="secondary">{getSubscriptionPlanLabel(planTransition.to)}</Badge>
          </span>
        ) : null}
      </TableCell>
      {isCompact ? <CompactAmountCell event={event} /> : <MrrImpactAndAfterCells event={event} />}
    </TableRow>
  );
}

function CompactAmountCell({ event }: Readonly<{ event: BillingEventSummary }>) {
  const isNegativeAmount = event.amountDelta != null && event.amountDelta < 0;
  return (
    <TableCell
      className={`text-right whitespace-nowrap tabular-nums ${isNegativeAmount ? "text-rose-700 dark:text-rose-300" : ""}`}
    >
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
      <TableCell
        className={`text-right whitespace-nowrap tabular-nums ${isNegativeAmount ? "text-rose-700 dark:text-rose-300" : ""}`}
      >
        {event.amountDelta != null && event.currency ? formatCurrency(event.amountDelta, event.currency) : "—"}
      </TableCell>
      <TableCell className="hidden text-right whitespace-nowrap text-muted-foreground tabular-nums md:table-cell">
        {event.newAmount != null && event.currency ? formatCurrency(event.newAmount, event.currency) : "—"}
      </TableCell>
    </>
  );
}
