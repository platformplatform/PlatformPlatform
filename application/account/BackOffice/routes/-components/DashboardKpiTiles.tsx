import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { Card } from "@repo/ui/components/Card";
import { Skeleton } from "@repo/ui/components/Skeleton";
import { formatCurrency } from "@repo/utils/currency/formatCurrency";
import { Link } from "@tanstack/react-router";
import { ActivityIcon, BuildingIcon, CoinsIcon, UsersIcon } from "lucide-react";

import { api, DashboardTrendPeriod } from "@/shared/lib/api/client";

interface DashboardKpiTilesProps {
  period: DashboardTrendPeriod;
}

export function DashboardKpiTiles({ period }: Readonly<DashboardKpiTilesProps>) {
  const { data, isLoading } = api.useQuery("get", "/api/back-office/dashboard/kpis", {
    params: { query: { Period: period } }
  });

  const periodDays = periodToDays(period);

  return (
    <div className="grid grid-cols-[repeat(auto-fit,minmax(13rem,1fr))] gap-4">
      <KpiTile
        label={t`Total tenants`}
        icon={BuildingIcon}
        value={data?.totalTenants}
        loading={isLoading}
        subtitle={
          data ? (
            <Trans>
              +{data.newTenantsInPeriod} new in last {periodDays} days
            </Trans>
          ) : undefined
        }
        to="/accounts"
      />

      <KpiTile
        label={t`Blended MRR`}
        icon={CoinsIcon}
        value={data ? formatCurrency(data.blendedMonthlyRecurringRevenue, data.currency) : undefined}
        loading={isLoading}
        subtitle={
          data && data.blendedMonthlyRecurringRevenueDeltaPercent !== null ? (
            <DeltaSubtitle deltaPercent={data.blendedMonthlyRecurringRevenueDeltaPercent} />
          ) : undefined
        }
      />

      <KpiTile
        label={t`Users active`}
        icon={UsersIcon}
        value={data?.activeUsersInPeriod}
        loading={isLoading}
        subtitle={<Trans>Last {periodDays} days</Trans>}
        to="/users"
      />

      <KpiTile
        label={t`Active sessions`}
        icon={ActivityIcon}
        value={data?.activeSessionsLast24Hours}
        loading={isLoading}
        subtitle={<Trans>Last 24 hours</Trans>}
      />
    </div>
  );
}

function periodToDays(period: DashboardTrendPeriod): number {
  switch (period) {
    case DashboardTrendPeriod.Last7Days:
      return 7;
    case DashboardTrendPeriod.Last30Days:
      return 30;
    case DashboardTrendPeriod.Last90Days:
      return 90;
  }
}

function DeltaSubtitle({ deltaPercent }: Readonly<{ deltaPercent: number }>) {
  const positive = deltaPercent >= 0;
  const className = positive ? "text-emerald-500" : "text-rose-500";
  const sign = positive ? "+" : "";
  return (
    <span className={className}>
      {sign}
      {deltaPercent}% <Trans>vs prior period</Trans>
    </span>
  );
}

interface KpiTileProps {
  label: string;
  icon: React.ComponentType<{ className?: string; "aria-hidden"?: boolean | "true" | "false" }>;
  value: React.ReactNode;
  loading: boolean;
  subtitle?: React.ReactNode;
  to?: "/accounts" | "/users";
}

function KpiTile({ label, icon: Icon, value, loading, subtitle, to }: Readonly<KpiTileProps>) {
  const content = (
    <>
      <span className="flex items-center gap-2 text-xs font-semibold tracking-wider text-muted-foreground uppercase">
        <Icon className="size-4" aria-hidden="true" />
        {label}
      </span>
      {loading ? (
        <>
          <Skeleton className="h-7 w-24" />
          <Skeleton className="h-4 w-32" />
        </>
      ) : (
        <>
          <span className="text-2xl font-semibold tabular-nums">{value ?? "-"}</span>
          {subtitle && <span className="text-xs text-muted-foreground">{subtitle}</span>}
        </>
      )}
    </>
  );

  if (to) {
    return (
      <Card className="gap-2 rounded-lg p-0 shadow-none transition-colors hover:bg-accent">
        <Link to={to} className="flex flex-col gap-2 rounded-lg p-4">
          {content}
        </Link>
      </Card>
    );
  }

  return <Card className="gap-2 rounded-lg p-4 shadow-none">{content}</Card>;
}
