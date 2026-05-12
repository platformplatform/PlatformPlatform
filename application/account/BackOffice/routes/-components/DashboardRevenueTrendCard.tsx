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
import { formatCurrency } from "@repo/utils/currency/formatCurrency";

import type { DashboardTrendPeriod } from "@/shared/lib/api/client";

import { api } from "@/shared/lib/api/client";

import { DashboardCardShell } from "./DashboardCardShell";

interface DashboardRevenueTrendCardProps {
  period: DashboardTrendPeriod;
}

export function DashboardRevenueTrendCard({ period }: Readonly<DashboardRevenueTrendCardProps>) {
  const { i18n } = useLingui();
  const { data, isLoading } = api.useQuery("get", "/api/back-office/dashboard/revenue-trend", {
    params: { query: { Period: period } }
  });

  const points = data?.points ?? [];
  const priorPoints = data?.priorPoints ?? [];
  const chartData = points.map((point, index) => ({
    date: point.date,
    current: point.revenue,
    prior: priorPoints[index]?.revenue ?? 0
  }));
  const currency = data?.currency ?? null;
  const dateFormatter = new Intl.DateTimeFormat(i18n.locale, { month: "short", day: "numeric" });
  const compactNumberFormatter = new Intl.NumberFormat(i18n.locale, { notation: "compact", maximumFractionDigits: 1 });

  const currentGain = points.length > 0 ? points[points.length - 1].revenue - points[0].revenue : 0;
  const priorGain = priorPoints.length > 0 ? priorPoints[priorPoints.length - 1].revenue - priorPoints[0].revenue : 0;
  const deltaPercent = priorGain === 0 ? null : Math.round(((currentGain - priorGain) / priorGain) * 100);

  return (
    <DashboardCardShell
      title={<Trans>Revenue</Trans>}
      subtitle={
        data && currency && deltaPercent !== null ? (
          <Trans>
            {formatCurrency(currentGain, currency)} this period · {formatDelta(deltaPercent)} vs prior period
          </Trans>
        ) : data && currency ? (
          <Trans>{formatCurrency(currentGain, currency)} this period, excluding VAT</Trans>
        ) : undefined
      }
    >
      {isLoading ? (
        <Skeleton className="h-[14rem] w-full" />
      ) : (
        <ResponsiveContainer width="100%" height={224}>
          <AreaChart accessibilityLayer={true} data={chartData} margin={{ left: 8, right: 8, top: 12, bottom: 0 }}>
            <defs>
              <linearGradient id="dashboard-revenue-trend-fill" x1="0" y1="0" x2="0" y2="1">
                <stop offset="5%" stopColor="var(--chart-2)" stopOpacity={0.8} />
                <stop offset="95%" stopColor="var(--chart-2)" stopOpacity={0.1} />
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
              width={56}
              stroke="var(--muted-foreground)"
              tickFormatter={(value: number) => compactNumberFormatter.format(value)}
            />
            <Tooltip
              cursor={false}
              labelFormatter={(value) => dateFormatter.format(new Date(value as string))}
              formatter={(value, name) => [currency ? formatCurrency(Number(value), currency) : String(value), name]}
              contentStyle={{
                backgroundColor: "var(--popover)",
                borderColor: "var(--border)",
                borderRadius: "0.5rem",
                color: "var(--popover-foreground)"
              }}
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
              fill="url(#dashboard-revenue-trend-fill)"
              stroke="var(--chart-2)"
              strokeWidth={2}
            />
          </AreaChart>
        </ResponsiveContainer>
      )}
    </DashboardCardShell>
  );
}

function formatDelta(deltaPercent: number): string {
  const sign = deltaPercent >= 0 ? "+" : "";
  return `${sign}${deltaPercent}%`;
}
