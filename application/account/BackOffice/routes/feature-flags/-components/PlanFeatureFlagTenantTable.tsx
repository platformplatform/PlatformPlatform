import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { Badge } from "@repo/ui/components/Badge";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@repo/ui/components/Table";
import { TenantLogo } from "@repo/ui/components/TenantLogo";
import { useFormatDate } from "@repo/ui/hooks/useSmartDate";
import { useNavigate } from "@tanstack/react-router";

import { getSubscriptionPlanLabel } from "@/shared/lib/api/labels";
import { getSubscriptionPlanBadgeClass } from "@/shared/lib/planBadge";

import type { FeatureFlagTenantInfo } from "./types";

import { MrrCell } from "../../accounts/-components/MrrCell";
import { TenantStatusBadge } from "../../accounts/-components/TenantStatusBadge";
import { getUserDisplayName } from "../../users/-components/userDisplay";

export function PlanFeatureFlagTenantTable({
  ariaLabel,
  tenants
}: Readonly<{ ariaLabel: string; tenants: FeatureFlagTenantInfo[] }>) {
  return (
    <Table rowSize="compact" aria-label={ariaLabel} className="w-full table-fixed" containerClassName="overflow-x-clip">
      <TableHeader>
        <TableRow>
          <TableHead>
            <Trans>Account</Trans>
          </TableHead>
          <TableHead className="hidden w-[6rem] md:table-cell">
            <Trans>Plan</Trans>
          </TableHead>
          <TableHead className="hidden w-[6rem] lg:table-cell">
            <Trans>MRR</Trans>
          </TableHead>
          <TableHead className="hidden w-[7rem] lg:table-cell">
            <Trans>Renewal</Trans>
          </TableHead>
          <TableHead className="hidden w-[6rem] md:table-cell">
            <Trans>Status</Trans>
          </TableHead>
          <TableHead className="w-[6rem] text-right">
            <Trans>State</Trans>
          </TableHead>
        </TableRow>
      </TableHeader>
      <TableBody>
        {tenants.map((tenant) => (
          <PlanFeatureFlagTenantRow key={tenant.id} tenant={tenant} />
        ))}
      </TableBody>
    </Table>
  );
}

function PlanFeatureFlagTenantRow({ tenant }: Readonly<{ tenant: FeatureFlagTenantInfo }>) {
  const formatDate = useFormatDate();
  const navigate = useNavigate();
  const ownerLabel = tenant.owner
    ? getUserDisplayName(tenant.owner.firstName, tenant.owner.lastName, tenant.owner.email)
    : null;

  return (
    <TableRow
      rowKey={tenant.id}
      className="cursor-pointer"
      onClick={() =>
        navigate({ to: "/accounts/$tenantId", params: { tenantId: tenant.id }, search: { tab: "feature-flags" } })
      }
    >
      <TableCell>
        <div className="flex min-w-0 items-center gap-3">
          <TenantLogo logoUrl={tenant.logoUrl} tenantName={tenant.name} size="md" className="size-9 shrink-0" />
          <div className="flex min-w-0 flex-col gap-0.5">
            <span className="truncate font-medium text-foreground">{tenant.name}</span>
            {ownerLabel && <span className="truncate text-xs text-muted-foreground">{ownerLabel}</span>}
          </div>
        </div>
      </TableCell>
      <TableCell className="hidden md:table-cell">
        <Badge className={getSubscriptionPlanBadgeClass(tenant.plan)}>{getSubscriptionPlanLabel(tenant.plan)}</Badge>
      </TableCell>
      <TableCell className="hidden tabular-nums lg:table-cell">
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
        <TenantStatusBadge
          plan={tenant.plan}
          plannedChange={tenant.plannedChange}
          hasEverSubscribed={tenant.hasEverSubscribed}
        />
      </TableCell>
      <TableCell className="text-right">
        <Badge variant={tenant.isEnabled ? "default" : "outline"}>{tenant.isEnabled ? t`Enabled` : t`Disabled`}</Badge>
      </TableCell>
    </TableRow>
  );
}
