import { t } from "@lingui/core/macro";
import { useLingui } from "@lingui/react";
import { Trans } from "@lingui/react/macro";
import {
  Area,
  AreaChart,
  CartesianGrid,
  Legend,
  Line,
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

interface DashboardUserLoginsCardProps {
  period: DashboardTrendPeriod;
}

export function DashboardUserLoginsCard({ period }: Readonly<DashboardUserLoginsCardProps>) {
  const { i18n } = useLingui();
  const { data, isLoading } = api.useQuery("get", "/api/back-office/dashboard/trends", {
    params: { query: { Metric: DashboardTrendMetric.LoginActivity, Period: period } }
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
  const average = points.length === 0 ? 0 : Math.round(total / points.length);
  const dateFormatter = new Intl.DateTimeFormat(i18n.locale, { month: "short", day: "numeric" });

  return (
    <DashboardCardShell
      title={<Trans>User logins / day</Trans>}
      subtitle={
        data ? (
          <Trans>
            {total} total · avg {average}/day
          </Trans>
        ) : undefined
      }
    >
      {isLoading ? (
        <Skeleton className="h-[14rem] w-full" />
      ) : (
        <ResponsiveContainer width="100%" height={224}>
          <AreaChart accessibilityLayer={true} data={chartData} margin={{ left: 8, right: 8, top: 12, bottom: 0 }}>
            <defs>
              <linearGradient id="dashboard-user-logins-fill" x1="0" y1="0" x2="0" y2="1">
                <stop offset="5%" stopColor="var(--chart-3)" stopOpacity={0.8} />
                <stop offset="95%" stopColor="var(--chart-3)" stopOpacity={0.1} />
              </linearGradient>
            </defs>
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
              cursor={false}
              content={
                <CurrentPriorTooltip
                  formatValue={(value) => new Intl.NumberFormat(i18n.locale).format(value)}
                  accentColor="var(--chart-3)"
                />
              }
            />
            <Legend wrapperStyle={{ fontSize: "0.75rem" }} iconType="line" />
            <Line
              dataKey="prior"
              name={t`Prior period`}
              type="monotone"
              stroke="var(--muted-foreground)"
              strokeOpacity={0.6}
              strokeDasharray="4 3"
              strokeWidth={1.5}
              dot={false}
            />
            <Area
              dataKey="current"
              name={t`Current period`}
              type="monotone"
              fill="url(#dashboard-user-logins-fill)"
              stroke="var(--chart-3)"
              strokeWidth={2}
            />
          </AreaChart>
        </ResponsiveContainer>
      )}
    </DashboardCardShell>
  );
}
