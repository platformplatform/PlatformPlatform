import { t } from "@lingui/core/macro";
import { useLingui } from "@lingui/react";
import { Trans } from "@lingui/react/macro";
import { Area, AreaChart, CartesianGrid, ResponsiveContainer, Tooltip, XAxis, YAxis } from "@repo/ui/components/Chart";
import { Skeleton } from "@repo/ui/components/Skeleton";
import { formatCurrency } from "@repo/utils/currency/formatCurrency";

import { api } from "@/shared/lib/api/client";

import { DashboardCardShell } from "./DashboardCardShell";

export function DashboardRevenueTrendCard() {
  const { i18n } = useLingui();
  const { data, isLoading } = api.useQuery("get", "/api/back-office/dashboard/revenue-trend", {});

  const points = data?.points ?? [];
  const chartData = points.map((point) => ({ date: point.date, revenue: point.revenue }));
  const currency = data?.currency ?? null;
  const dateFormatter = new Intl.DateTimeFormat(i18n.locale, { day: "numeric", month: "short" });
  const compactNumberFormatter = new Intl.NumberFormat(i18n.locale, { notation: "compact", maximumFractionDigits: 1 });

  const lifetimeRevenue = points.reduce((sum, point) => sum + point.revenue, 0);

  return (
    <DashboardCardShell
      title={<Trans>Revenue</Trans>}
      subtitle={
        currency ? <Trans>{formatCurrency(lifetimeRevenue, currency)} all-time, excluding VAT</Trans> : undefined
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
              formatter={(value) => [currency ? formatCurrency(Number(value), currency) : String(value), t`Revenue`]}
              contentStyle={{
                backgroundColor: "var(--popover)",
                borderColor: "var(--border)",
                borderRadius: "0.5rem",
                color: "var(--popover-foreground)"
              }}
            />
            <Area
              dataKey="revenue"
              name={t`Revenue`}
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
