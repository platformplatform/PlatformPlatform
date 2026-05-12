import { t } from "@lingui/core/macro";
import { useLingui } from "@lingui/react";
import { Trans } from "@lingui/react/macro";
import {
  Bar,
  BarChart,
  CartesianGrid,
  Legend,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis
} from "@repo/ui/components/Chart";
import { Skeleton } from "@repo/ui/components/Skeleton";

import type { DashboardTrendPeriod } from "@/shared/lib/api/client";

import { api, DashboardTrendMetric } from "@/shared/lib/api/client";

import { CurrentPriorTooltip } from "./CurrentPriorTooltip";
import { DashboardCardShell } from "./DashboardCardShell";

interface DashboardTenantGrowthCardProps {
  period: DashboardTrendPeriod;
}

export function DashboardTenantGrowthCard({ period }: Readonly<DashboardTenantGrowthCardProps>) {
  const { i18n } = useLingui();
  const { data, isLoading } = api.useQuery("get", "/api/back-office/dashboard/trends", {
    params: { query: { Metric: DashboardTrendMetric.NewTenants, Period: period } }
  });

  const points = data?.points ?? [];
  const priorPoints = data?.priorPoints ?? [];
  const chartData = points.map((point, index) => ({
    date: point.date,
    priorDate: priorPoints[index]?.date,
    current: point.value,
    prior: priorPoints[index]?.value ?? 0
  }));
  const total = points.reduce((acc, p) => acc + p.value, 0);
  const priorTotal = priorPoints.reduce((acc, p) => acc + p.value, 0);
  const dateFormatter = new Intl.DateTimeFormat(i18n.locale, { month: "short", day: "numeric" });

  return (
    <DashboardCardShell
      title={<Trans>Account growth</Trans>}
      subtitle={
        data ? (
          <Trans>
            {total} new signups · {priorTotal} prior period
          </Trans>
        ) : undefined
      }
    >
      {isLoading ? (
        <Skeleton className="h-[14rem] w-full" />
      ) : (
        <ResponsiveContainer width="100%" height={224}>
          <BarChart accessibilityLayer={true} data={chartData} margin={{ left: 8, right: 8, top: 12, bottom: 0 }}>
            <CartesianGrid vertical={false} stroke="var(--border)" strokeOpacity={0.5} />
            <XAxis
              dataKey="date"
              tickLine={false}
              axisLine={false}
              tickMargin={8}
              minTickGap={32}
              tickFormatter={(value: string) => dateFormatter.format(new Date(value))}
              stroke="var(--muted-foreground)"
            />
            <YAxis
              tickLine={false}
              axisLine={false}
              tickMargin={8}
              allowDecimals={false}
              width={56}
              stroke="var(--muted-foreground)"
            />
            <Tooltip
              cursor={{ fill: "var(--muted)", opacity: 0.3 }}
              content={
                <CurrentPriorTooltip
                  formatValue={(value) => new Intl.NumberFormat(i18n.locale).format(value)}
                  accentColor="var(--chart-2)"
                />
              }
            />
            <Legend wrapperStyle={{ fontSize: "0.75rem" }} iconType="circle" />
            <Bar
              dataKey="prior"
              name={t`Prior period`}
              fill="var(--muted-foreground)"
              fillOpacity={0.3}
              radius={[4, 4, 0, 0]}
            />
            <Bar dataKey="current" name={t`Current period`} fill="var(--chart-2)" radius={[4, 4, 0, 0]} />
          </BarChart>
        </ResponsiveContainer>
      )}
    </DashboardCardShell>
  );
}
