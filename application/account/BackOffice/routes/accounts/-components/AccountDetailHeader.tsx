import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { Badge } from "@repo/ui/components/Badge";
import { Button } from "@repo/ui/components/Button";
import { Skeleton } from "@repo/ui/components/Skeleton";
import { TenantLogo } from "@repo/ui/components/TenantLogo";
import { useFormatDate } from "@repo/ui/hooks/useSmartDate";
import { formatCurrency } from "@repo/utils/currency/formatCurrency";
import { Link } from "@tanstack/react-router";
import { ArrowLeftIcon, CalendarClockIcon, XCircleIcon } from "lucide-react";

import type { components } from "@/shared/lib/api/client";

import { PlannedSubscriptionChange, TenantState } from "@/shared/lib/api/client";
import { getSubscriptionPlanLabel } from "@/shared/lib/api/labels";
import { getSubscriptionPlanBadgeClass } from "@/shared/lib/planBadge";

function formatAmount(amount: number | null, currency: string | null): string {
  if (amount === null || currency === null) {
    return "-";
  }
  return formatCurrency(amount, currency);
}

type TenantDetailResponse = components["schemas"]["TenantDetailResponse"];
type TenantUserCountsResponse = components["schemas"]["TenantUserCountsResponse"];

interface AccountDetailHeaderProps {
  tenant: TenantDetailResponse | undefined;
  isLoading: boolean;
  userCounts: TenantUserCountsResponse | undefined;
  isLoadingUserCounts: boolean;
}

export function AccountDetailHeader({
  tenant,
  isLoading,
  userCounts,
  isLoadingUserCounts
}: Readonly<AccountDetailHeaderProps>) {
  const formatDate = useFormatDate();

  const plannedChange = ((): PlannedSubscriptionChange | null => {
    if (!tenant) return null;
    if (tenant.cancelAtPeriodEnd) return PlannedSubscriptionChange.Cancellation;
    if (tenant.scheduledPlan) return PlannedSubscriptionChange.ScheduledPlanChange;
    return null;
  })();

  return (
    <div className="flex flex-col gap-6">
      <div>
        <Button
          variant="ghost"
          size="sm"
          className="-ml-2 gap-1.5"
          render={<Link to="/accounts" aria-label={t`Back to accounts`} />}
        >
          <ArrowLeftIcon className="size-4" />
          <Trans>Accounts</Trans>
        </Button>
      </div>

      <div className="flex flex-wrap items-center gap-4">
        <TenantLogo logoUrl={tenant?.logoUrl} tenantName={tenant?.name ?? ""} size="lg" />
        <div className="flex min-w-0 flex-col gap-1">
          {isLoading || !tenant ? (
            <>
              <Skeleton className="h-6 w-48" />
              <Skeleton className="h-4 w-32" />
            </>
          ) : (
            <>
              <h1>{tenant.name}</h1>
              <div className="flex flex-wrap items-center gap-2">
                <Badge className={getSubscriptionPlanBadgeClass(tenant.plan)}>
                  {getSubscriptionPlanLabel(tenant.plan)}
                </Badge>
                {tenant.state === TenantState.Suspended && (
                  <Badge variant="outline" className="border-destructive/30 text-destructive">
                    <Trans>Suspended</Trans>
                  </Badge>
                )}
                {plannedChange === PlannedSubscriptionChange.Cancellation && (
                  <Badge variant="outline" className="gap-1 border-destructive/30 text-destructive">
                    <XCircleIcon className="size-3" />
                    <Trans>Cancelling at period end</Trans>
                  </Badge>
                )}
                {plannedChange === PlannedSubscriptionChange.ScheduledPlanChange && tenant.scheduledPlan && (
                  <Badge variant="outline" className="gap-1">
                    <CalendarClockIcon className="size-3" />
                    <Trans>Switching to {getSubscriptionPlanLabel(tenant.scheduledPlan)}</Trans>
                  </Badge>
                )}
              </div>
            </>
          )}
        </div>
      </div>

      <div className="grid grid-cols-2 gap-3 sm:grid-cols-4">
        <KpiTile label={t`MRR`} loading={isLoading}>
          {tenant ? formatAmount(tenant.monthlyRecurringRevenue, tenant.currency) : "-"}
        </KpiTile>
        <KpiTile label={t`Lifetime value`} loading={isLoading}>
          {tenant ? formatAmount(tenant.lifetimeValue, tenant.currency) : "-"}
        </KpiTile>
        <KpiTile label={t`Renewal`} loading={isLoading}>
          {tenant?.renewalDate ? formatDate(tenant.renewalDate) : "-"}
        </KpiTile>
        <KpiTile label={t`Users (active / total)`} loading={isLoadingUserCounts}>
          {userCounts ? `${userCounts.activeUsers} / ${userCounts.totalUsers}` : "-"}
        </KpiTile>
      </div>
    </div>
  );
}

function KpiTile({
  label,
  loading,
  children
}: Readonly<{ label: string; loading: boolean; children: React.ReactNode }>) {
  return (
    <div className="flex flex-col gap-1 rounded-lg border border-border bg-card p-4">
      <span className="text-xs text-muted-foreground">{label}</span>
      {loading ? (
        <Skeleton className="mt-1 h-6 w-20" />
      ) : (
        <span className="text-lg font-semibold tabular-nums">{children}</span>
      )}
    </div>
  );
}
