import { Badge } from "@repo/ui/components/Badge";
import { TableCell, TableRow } from "@repo/ui/components/Table";
import { TenantLogo } from "@repo/ui/components/TenantLogo";
import { getCountryFlagEmoji } from "@repo/ui/utils/countryFlag";

import type { components } from "@/shared/lib/api/client";

import { SmartDateTime } from "@/shared/components/SmartDateTime";
import { getSubscriptionPlanLabel } from "@/shared/lib/api/labels";
import { getSubscriptionPlanBadgeClass } from "@/shared/lib/planBadge";

import { getUserDisplayName } from "../../users/-components/userDisplay";
import { MrrCell } from "./MrrCell";
import { TenantStatusBadge } from "./TenantStatusBadge";

type TenantSummary = components["schemas"]["TenantSummary"];

export function AccountsTableRow({
  tenant,
  formatDate
}: Readonly<{
  tenant: TenantSummary;
  formatDate: (value: string | null | undefined, includeTime?: boolean, omitCurrentYear?: boolean) => string;
}>) {
  return (
    <TableRow rowKey={tenant.id}>
      <TableCell>
        <div className="flex min-w-0 items-center gap-3">
          <TenantLogo logoUrl={tenant.logoUrl} tenantName={tenant.name} size="md" className="size-10" />
          <div className="flex min-w-0 flex-col gap-1">
            <span className="truncate font-medium text-foreground">{tenant.name}</span>
            {tenant.owner && (
              <span className="truncate text-xs text-muted-foreground">
                {getUserDisplayName(tenant.owner.firstName, tenant.owner.lastName, tenant.owner.email)}
              </span>
            )}
            <div className="hidden md:flex lg:hidden">
              <Badge className={`w-fit ${getSubscriptionPlanBadgeClass(tenant.plan)}`}>
                {getSubscriptionPlanLabel(tenant.plan)}
              </Badge>
            </div>
            <div className="flex items-center justify-between gap-2 md:hidden">
              <div className="flex flex-wrap items-center gap-1.5">
                <Badge className={getSubscriptionPlanBadgeClass(tenant.plan)}>
                  {getSubscriptionPlanLabel(tenant.plan)}
                </Badge>
                <TenantStatusBadge
                  plan={tenant.plan}
                  plannedChange={tenant.plannedChange}
                  hasEverSubscribed={tenant.hasEverSubscribed}
                />
              </div>
              <div className="shrink-0 text-right text-sm text-muted-foreground tabular-nums">
                <MrrCell
                  monthlyRecurringRevenue={tenant.monthlyRecurringRevenue}
                  scheduledPriceAmount={tenant.scheduledPriceAmount}
                  currency={tenant.currency}
                  plannedChange={tenant.plannedChange}
                  align="end"
                />
              </div>
            </div>
          </div>
        </div>
      </TableCell>
      <TableCell className="hidden lg:table-cell">
        <Badge className={getSubscriptionPlanBadgeClass(tenant.plan)}>{getSubscriptionPlanLabel(tenant.plan)}</Badge>
      </TableCell>
      <TableCell className="hidden tabular-nums md:table-cell">
        <MrrCell
          monthlyRecurringRevenue={tenant.monthlyRecurringRevenue}
          scheduledPriceAmount={tenant.scheduledPriceAmount}
          currency={tenant.currency}
          plannedChange={tenant.plannedChange}
        />
      </TableCell>
      <TableCell className="hidden lg:table-cell">
        {tenant.renewalDate ? formatDate(tenant.renewalDate) : <span className="text-muted-foreground">-</span>}
      </TableCell>
      <TableCell className="hidden md:table-cell">
        <div className="flex flex-col gap-1">
          <TenantStatusBadge
            plan={tenant.plan}
            plannedChange={tenant.plannedChange}
            hasEverSubscribed={tenant.hasEverSubscribed}
          />
          {tenant.renewalDate && (
            <span className="text-xs text-muted-foreground tabular-nums lg:hidden">
              {formatDate(tenant.renewalDate)}
            </span>
          )}
        </div>
      </TableCell>
      <TableCell className="hidden xl:table-cell">
        {tenant.country ? (
          <span className="flex items-center gap-1.5">
            <span aria-hidden={true}>{getCountryFlagEmoji(tenant.country)}</span>
            <span>{tenant.country}</span>
          </span>
        ) : (
          "-"
        )}
      </TableCell>
      <TableCell className="hidden xl:table-cell">
        <div className="flex flex-col leading-tight">
          <SmartDateTime date={tenant.createdAt} />
          <span className="text-xs text-muted-foreground tabular-nums">{formatDate(tenant.createdAt, true, true)}</span>
        </div>
      </TableCell>
    </TableRow>
  );
}
