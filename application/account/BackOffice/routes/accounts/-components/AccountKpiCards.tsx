import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { Card } from "@repo/ui/components/Card";
import { Progress } from "@repo/ui/components/Progress";
import { Skeleton } from "@repo/ui/components/Skeleton";
import { useFormatDate } from "@repo/ui/hooks/useSmartDate";
import { formatCurrency } from "@repo/utils/currency/formatCurrency";

import type { components } from "@/shared/lib/api/client";

import { api } from "@/shared/lib/api/client";

type TenantDetailResponse = components["schemas"]["TenantDetailResponse"];

interface AccountKpiCardsProps {
  tenant: TenantDetailResponse | undefined;
  tenantId: string;
  isLoading: boolean;
}

function formatAmount(amount: number | null, currency: string | null): string {
  if (amount === null || currency === null) {
    return "-";
  }
  return formatCurrency(amount, currency);
}

export function AccountKpiCards({ tenant, tenantId, isLoading }: Readonly<AccountKpiCardsProps>) {
  const formatDate = useFormatDate();
  const userCountsQuery = api.useQuery("get", "/api/back-office/tenants/{id}/user-counts", {
    params: { path: { id: tenantId } }
  });
  const userCounts = userCountsQuery.data;
  const totalUsers = userCounts?.totalUsers ?? 0;
  const activeUsers = userCounts?.activeUsers ?? 0;
  const activationPercent = totalUsers === 0 ? 0 : Math.round((activeUsers / totalUsers) * 100);

  return (
    <div className="grid grid-cols-[repeat(auto-fit,minmax(13rem,1fr))] gap-4">
      <KpiCard
        label={t`MRR`}
        loading={isLoading}
        subtitle={tenant?.renewalDate ? <Trans>Renews {formatDate(tenant.renewalDate)}</Trans> : undefined}
      >
        <MrrAmount tenant={tenant} />
      </KpiCard>

      <KpiCard
        label={t`Lifetime value`}
        loading={isLoading}
        subtitle={tenant ? <Trans>Since {formatDate(tenant.createdAt)}</Trans> : undefined}
      >
        <span className="text-2xl font-semibold tabular-nums">
          {tenant ? formatAmount(tenant.lifetimeValue, tenant.currency) : "-"}
        </span>
      </KpiCard>

      <KpiCard
        label={t`Users`}
        loading={isLoading || userCountsQuery.isLoading}
        subtitle={userCounts ? <Trans>{activationPercent}% activation</Trans> : undefined}
      >
        <div className="flex flex-col gap-2">
          <span className="text-2xl font-semibold tabular-nums">
            {userCounts ? `${activeUsers} / ${totalUsers}` : "-"}
          </span>
          {userCounts && <Progress value={activationPercent} variant="success" />}
        </div>
      </KpiCard>
    </div>
  );
}

function MrrAmount({ tenant }: Readonly<{ tenant: TenantDetailResponse | undefined }>) {
  if (!tenant) {
    return <span className="text-2xl font-semibold tabular-nums">-</span>;
  }

  const currentAmount = formatAmount(tenant.monthlyRecurringRevenue, tenant.currency);
  const isCanceling = tenant.cancelAtPeriodEnd;
  const isDowngrading = !isCanceling && tenant.scheduledPlan !== null;
  const newAmount =
    isCanceling && tenant.currency !== null
      ? formatAmount(0, tenant.currency)
      : isDowngrading && tenant.scheduledPriceAmount !== null
        ? formatAmount(tenant.scheduledPriceAmount, tenant.currency)
        : null;

  if (newAmount === null) {
    return <span className="text-2xl font-semibold tabular-nums">{currentAmount}</span>;
  }

  return (
    <div className="flex flex-col leading-tight">
      <span className="text-sm text-muted-foreground tabular-nums line-through">{currentAmount}</span>
      <span className="text-2xl font-semibold tabular-nums">{newAmount}</span>
    </div>
  );
}

function KpiCard({
  label,
  loading,
  subtitle,
  children
}: Readonly<{
  label: string;
  loading: boolean;
  subtitle?: React.ReactNode;
  children: React.ReactNode;
}>) {
  return (
    <Card className="gap-2 rounded-lg p-4 py-4 shadow-none">
      <span className="text-xs font-semibold tracking-wider text-muted-foreground uppercase">{label}</span>
      {loading ? (
        <>
          <Skeleton className="h-7 w-24" />
          <Skeleton className="h-4 w-20" />
        </>
      ) : (
        <>
          {children}
          {subtitle && <span className="text-xs text-muted-foreground">{subtitle}</span>}
        </>
      )}
    </Card>
  );
}
