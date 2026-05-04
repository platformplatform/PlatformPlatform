import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { Card } from "@repo/ui/components/Card";
import { Skeleton } from "@repo/ui/components/Skeleton";
import { formatCurrency } from "@repo/utils/currency/formatCurrency";
import { Link } from "@tanstack/react-router";
import { ActivityIcon, BuildingIcon, CoinsIcon, UserPlusIcon, UsersIcon, UsersRoundIcon } from "lucide-react";

import { api } from "@/shared/lib/api/client";

export function DashboardKpiCards() {
  const { data, isLoading } = api.useQuery("get", "/api/back-office/dashboard/kpis");

  return (
    <div className="grid grid-cols-[repeat(auto-fit,minmax(13rem,1fr))] gap-4">
      <KpiCard
        label={t`Total tenants`}
        icon={BuildingIcon}
        value={data?.totalTenants}
        loading={isLoading}
        subtitle={
          data ? (
            <Trans>
              {data.activeTenants} active · {data.trialTenants} trial · {data.canceledTenants} canceled
            </Trans>
          ) : undefined
        }
        to="/accounts"
      />

      <KpiCard label={t`Total users`} icon={UsersIcon} value={data?.totalUsers} loading={isLoading} to="/users" />

      <KpiCard
        label={t`Monthly recurring revenue`}
        icon={CoinsIcon}
        value={data ? formatCurrency(data.totalMonthlyRecurringRevenue, data.currency) : undefined}
        loading={isLoading}
      />

      <KpiCard
        label={t`Active sessions`}
        icon={ActivityIcon}
        value={data?.activeSessionsLast24Hours}
        loading={isLoading}
        subtitle={<Trans>Last 24 hours</Trans>}
      />

      <KpiCard
        label={t`New tenants`}
        icon={UserPlusIcon}
        value={data?.newTenantsLast30Days}
        loading={isLoading}
        subtitle={<Trans>Last 30 days</Trans>}
        to="/accounts"
      />

      <KpiCard
        label={t`New users`}
        icon={UsersRoundIcon}
        value={data?.newUsersLast30Days}
        loading={isLoading}
        subtitle={<Trans>Last 30 days</Trans>}
        to="/users"
      />
    </div>
  );
}

interface KpiCardProps {
  label: string;
  icon: React.ComponentType<{ className?: string; "aria-hidden"?: boolean | "true" | "false" }>;
  value: React.ReactNode;
  loading: boolean;
  subtitle?: React.ReactNode;
  to?: "/accounts" | "/users";
}

function KpiCard({ label, icon: Icon, value, loading, subtitle, to }: Readonly<KpiCardProps>) {
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

  return <Card className="gap-2 rounded-lg p-4 py-4 shadow-none">{content}</Card>;
}
