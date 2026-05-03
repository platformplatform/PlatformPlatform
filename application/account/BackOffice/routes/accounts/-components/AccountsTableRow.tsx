import { Trans } from "@lingui/react/macro";
import { Badge } from "@repo/ui/components/Badge";
import { TableCell, TableRow } from "@repo/ui/components/Table";
import { TenantLogo } from "@repo/ui/components/TenantLogo";
import { getCountryFlagEmoji } from "@repo/ui/utils/countryFlag";
import { formatCurrency } from "@repo/utils/currency/formatCurrency";
import { CalendarClockIcon, XCircleIcon } from "lucide-react";

import type { components } from "@/shared/lib/api/client";

import { PlannedSubscriptionChange } from "@/shared/lib/api/client";
import { getSubscriptionPlanLabel } from "@/shared/lib/api/labels";
import { getSubscriptionPlanBadgeClass } from "@/shared/lib/planBadge";

type TenantSummary = components["schemas"]["TenantSummary"];

function formatMonthlyRevenue(amount: number | null, currency: string | null): string {
  if (amount === null || currency === null) {
    return "-";
  }
  return formatCurrency(amount, currency);
}

export function AccountsTableRow({
  tenant,
  formatDate
}: Readonly<{
  tenant: TenantSummary;
  formatDate: (value: string | null | undefined) => string;
}>) {
  return (
    <TableRow rowKey={tenant.id}>
      <TableCell>
        <div className="flex min-w-0 items-center gap-3">
          <TenantLogo tenantName={tenant.name} size="md" />
          <div className="flex min-w-0 flex-col gap-0.5">
            <span className="truncate font-medium text-foreground">{tenant.name}</span>
            <span className="text-sm text-muted-foreground md:hidden">
              {getSubscriptionPlanLabel(tenant.plan)} ·{" "}
              {formatMonthlyRevenue(tenant.monthlyRecurringRevenue, tenant.currency)}
            </span>
          </div>
        </div>
      </TableCell>
      <TableCell className="hidden md:table-cell">
        <Badge className={getSubscriptionPlanBadgeClass(tenant.plan)}>{getSubscriptionPlanLabel(tenant.plan)}</Badge>
      </TableCell>
      <TableCell className="hidden tabular-nums md:table-cell">
        {formatMonthlyRevenue(tenant.monthlyRecurringRevenue, tenant.currency)}
      </TableCell>
      <TableCell className="hidden lg:table-cell">
        <RenewalCell renewalDate={tenant.renewalDate} plannedChange={tenant.plannedChange} formatDate={formatDate} />
      </TableCell>
      <TableCell className="hidden lg:table-cell">
        {tenant.country ? (
          <span className="flex items-center gap-1.5">
            <span aria-hidden={true}>{getCountryFlagEmoji(tenant.country)}</span>
            <span>{tenant.country}</span>
          </span>
        ) : (
          "-"
        )}
      </TableCell>
      <TableCell className="hidden xl:table-cell">{formatDate(tenant.createdAt)}</TableCell>
    </TableRow>
  );
}

function RenewalCell({
  renewalDate,
  plannedChange,
  formatDate
}: Readonly<{
  renewalDate: string | null;
  plannedChange: PlannedSubscriptionChange | null;
  formatDate: (value: string | null | undefined) => string;
}>) {
  if (!renewalDate) {
    return <span className="text-muted-foreground">-</span>;
  }
  return (
    <div className="flex items-center gap-2">
      <span>{formatDate(renewalDate)}</span>
      {plannedChange === PlannedSubscriptionChange.Cancellation && (
        <Badge variant="outline" className="gap-1 border-destructive/30 text-destructive">
          <XCircleIcon className="size-3" />
          <Trans>Cancelling</Trans>
        </Badge>
      )}
      {plannedChange === PlannedSubscriptionChange.ScheduledPlanChange && (
        <Badge variant="outline" className="gap-1">
          <CalendarClockIcon className="size-3" />
          <Trans>Plan change</Trans>
        </Badge>
      )}
    </div>
  );
}
