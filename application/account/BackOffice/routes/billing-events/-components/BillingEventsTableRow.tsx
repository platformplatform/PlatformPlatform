import { Badge } from "@repo/ui/components/Badge";
import { TableCell, TableRow } from "@repo/ui/components/Table";
import { TenantLogo } from "@repo/ui/components/TenantLogo";
import { getCountryFlagEmoji } from "@repo/ui/utils/countryFlag";
import { formatCurrency } from "@repo/utils/currency/formatCurrency";

import type { components } from "@/shared/lib/api/client";

import { getBillingEventTypeLabel, getSubscriptionPlanLabel } from "@/shared/lib/api/labels";

type BillingEventSummary = components["schemas"]["BillingEventSummary"];

export function BillingEventsTableRow({
  event,
  formatDate
}: Readonly<{
  event: BillingEventSummary;
  formatDate: (value: string | null | undefined) => string;
}>) {
  return (
    <TableRow rowKey={event.id}>
      <TableCell>
        <div className="flex min-w-0 items-center gap-3">
          <TenantLogo logoUrl={event.tenantLogoUrl} tenantName={event.tenantName} size="sm" className="size-8" />
          <span className="truncate font-medium text-foreground">{event.tenantName}</span>
        </div>
      </TableCell>
      <TableCell>
        <Badge variant="outline">{getBillingEventTypeLabel(event.eventType)}</Badge>
      </TableCell>
      <TableCell className="hidden md:table-cell">
        {event.fromPlan != null && event.toPlan != null && event.fromPlan !== event.toPlan ? (
          <span className="text-sm text-muted-foreground">
            {getSubscriptionPlanLabel(event.fromPlan)} → {getSubscriptionPlanLabel(event.toPlan)}
          </span>
        ) : event.toPlan != null ? (
          <Badge variant="secondary">{getSubscriptionPlanLabel(event.toPlan)}</Badge>
        ) : (
          <span className="text-muted-foreground">—</span>
        )}
      </TableCell>
      <TableCell className="hidden tabular-nums md:table-cell">
        {event.amountDelta != null && event.currency ? (
          formatCurrency(event.amountDelta, event.currency)
        ) : (
          <span className="text-muted-foreground">—</span>
        )}
      </TableCell>
      <TableCell className="hidden xl:table-cell">
        {event.country ? (
          <span className="flex items-center gap-1.5">
            <span aria-hidden={true}>{getCountryFlagEmoji(event.country)}</span>
            <span>{event.country}</span>
          </span>
        ) : (
          <span className="text-muted-foreground">—</span>
        )}
      </TableCell>
      <TableCell className="text-sm text-muted-foreground tabular-nums">{formatDate(event.occurredAt)}</TableCell>
    </TableRow>
  );
}
