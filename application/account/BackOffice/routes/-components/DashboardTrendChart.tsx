import { t } from "@lingui/core/macro";
import { useLingui } from "@lingui/react";
import { type ChartConfig, ChartContainer, ChartTooltip, ChartTooltipContent } from "@repo/ui/components/Chart";
import { Skeleton } from "@repo/ui/components/Skeleton";
import { Area, AreaChart, CartesianGrid, XAxis, YAxis } from "recharts";

import type { components, DashboardTrendPeriod } from "@/shared/lib/api/client";

import { api, DashboardTrendMetric } from "@/shared/lib/api/client";

type TrendPoint = components["schemas"]["BackOfficeDashboardTrendPoint"];

interface DashboardTrendChartProps {
  metric: DashboardTrendMetric;
  period: DashboardTrendPeriod;
}

export function DashboardTrendChart({ metric, period }: Readonly<DashboardTrendChartProps>) {
  const { i18n } = useLingui();
  const { data, isLoading } = api.useQuery("get", "/api/back-office/dashboard/trends", {
    params: { query: { Metric: metric, Period: period } }
  });

  const chartConfig = {
    value: { label: getMetricLabel(metric), color: "var(--chart-1)" }
  } satisfies ChartConfig;

  if (isLoading) {
    return <Skeleton className="aspect-video w-full" />;
  }

  const points = data?.points ?? [];
  const chartData = points.map((point: TrendPoint) => ({ date: point.date, value: point.value }));
  const dateFormatter = new Intl.DateTimeFormat(i18n.locale, { month: "short", day: "numeric" });

  return (
    <ChartContainer config={chartConfig} className="aspect-auto h-[18rem] w-full">
      <AreaChart data={chartData} margin={{ left: 8, right: 8, top: 12, bottom: 0 }}>
        <defs>
          <linearGradient id="dashboard-trend-fill" x1="0" y1="0" x2="0" y2="1">
            <stop offset="5%" stopColor="var(--color-value)" stopOpacity={0.4} />
            <stop offset="95%" stopColor="var(--color-value)" stopOpacity={0.05} />
          </linearGradient>
        </defs>
        <CartesianGrid vertical={false} />
        <XAxis
          dataKey="date"
          tickLine={false}
          axisLine={false}
          tickMargin={8}
          minTickGap={32}
          tickFormatter={(value: string) => dateFormatter.format(new Date(value))}
        />
        <YAxis tickLine={false} axisLine={false} tickMargin={8} allowDecimals={false} width={32} />
        <ChartTooltip
          cursor={false}
          content={
            <ChartTooltipContent
              labelFormatter={(value) => dateFormatter.format(new Date(value as string))}
              indicator="dot"
            />
          }
        />
        <Area
          dataKey="value"
          type="monotone"
          fill="url(#dashboard-trend-fill)"
          stroke="var(--color-value)"
          strokeWidth={2}
        />
      </AreaChart>
    </ChartContainer>
  );
}

function getMetricLabel(metric: DashboardTrendMetric): string {
  switch (metric) {
    case DashboardTrendMetric.NewTenants:
      return t`New tenants`;
    case DashboardTrendMetric.NewUsers:
      return t`New users`;
    case DashboardTrendMetric.LoginActivity:
      return t`Login activity`;
  }
}
