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

import { CurrentPriorTooltip } from "./CurrentPriorTooltip";
import { DashboardCardShell } from "./DashboardCardShell";
import { DeltaPercent } from "./DeltaPercent";

interface DashboardMrrTrendCardProps {
  period: DashboardTrendPeriod;
}

export function DashboardMrrTrendCard({ period }: Readonly<DashboardMrrTrendCardProps>) {
  const { i18n } = useLingui();
  const { data, isLoading } = api.useQuery("get", "/api/back-office/dashboard/mrr-trend", {
    params: { query: { Period: period } }
  });

  const points = data?.points ?? [];
  const priorPoints = data?.priorPoints ?? [];
  const chartData = points.map((point, index) => ({
    date: point.date,
    priorDate: priorPoints[index]?.date,
    current: point.monthlyRecurringRevenue,
    prior: priorPoints[index]?.monthlyRecurringRevenue ?? 0
  }));
  const currency = data?.currency ?? null;
  const dateFormatter = new Intl.DateTimeFormat(i18n.locale, { month: "short", day: "numeric" });
  const compactNumberFormatter = new Intl.NumberFormat(i18n.locale, { notation: "compact", maximumFractionDigits: 1 });

  const blended = points.length > 0 ? points[points.length - 1].monthlyRecurringRevenue : 0;
  const first = points.length > 0 ? points[0].monthlyRecurringRevenue : 0;
  const deltaPercent = first === 0 ? null : ((blended - first) / first) * 100;

  return (
    <DashboardCardShell
      title={<Trans>MRR trend</Trans>}
      subtitle={
        data && currency && deltaPercent !== null ? (
          <span>
            {formatCurrency(blended, currency)} <Trans>blended</Trans> · <DeltaPercent value={deltaPercent} />{" "}
            <Trans>over period</Trans>
          </span>
        ) : data && currency ? (
          <Trans>{formatCurrency(blended, currency)} blended</Trans>
        ) : undefined
      }
    >
      {isLoading ? (
        <Skeleton className="h-[14rem] w-full" />
      ) : (
        <ResponsiveContainer width="100%" height={224}>
          <AreaChart accessibilityLayer={true} data={chartData} margin={{ left: 8, right: 8, top: 12, bottom: 0 }}>
            <defs>
              <linearGradient id="dashboard-mrr-trend-fill" x1="0" y1="0" x2="0" y2="1">
                <stop offset="5%" stopColor="var(--chart-1)" stopOpacity={0.8} />
                <stop offset="95%" stopColor="var(--chart-1)" stopOpacity={0.1} />
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
              content={
                <CurrentPriorTooltip
                  formatValue={(value) => (currency ? formatCurrency(value, currency) : String(value))}
                  accentColor="var(--chart-1)"
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
              fill="url(#dashboard-mrr-trend-fill)"
              stroke="var(--chart-1)"
              strokeWidth={2}
            />
          </AreaChart>
        </ResponsiveContainer>
      )}
    </DashboardCardShell>
  );
}
