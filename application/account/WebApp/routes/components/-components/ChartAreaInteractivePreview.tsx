import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@repo/ui/components/Card";
import {
  Area,
  AreaChart,
  CartesianGrid,
  type ChartConfig,
  ChartContainer,
  ChartLegend,
  ChartLegendContent,
  ChartTooltip,
  ChartTooltipContent,
  XAxis
} from "@repo/ui/components/Chart";
import { SelectContent, SelectItem, SelectTrigger, SelectValue } from "@repo/ui/components/Select";
import { SelectField } from "@repo/ui/components/SelectField";
import { useState } from "react";

import { dailyVisitors as chartData } from "./chartSampleData";

const shortDateFormatter = new Intl.DateTimeFormat("en-US", { month: "short", day: "numeric" });

export function ChartAreaInteractivePreview() {
  const [timeRange, setTimeRange] = useState<string>("90d");
  const chartConfig = {
    visitors: { label: t`Visitors` },
    desktop: { label: t`Desktop`, color: "var(--chart-1)" },
    mobile: { label: t`Mobile`, color: "var(--chart-2)" }
  } satisfies ChartConfig;

  const filteredData = chartData.filter((item) => {
    const date = new Date(item.date);
    const referenceDate = new Date("2024-06-30");
    let daysToSubtract = 90;
    if (timeRange === "30d") {
      daysToSubtract = 30;
    } else if (timeRange === "7d") {
      daysToSubtract = 7;
    }
    const startDate = new Date(referenceDate);
    startDate.setDate(startDate.getDate() - daysToSubtract);
    return date >= startDate;
  });

  const timeRangeItems = [
    { value: "90d", label: t`Last 3 months` },
    { value: "30d", label: t`Last 30 days` },
    { value: "7d", label: t`Last 7 days` }
  ];

  return (
    <Card className="pt-0">
      <CardHeader className="flex items-center gap-2 space-y-0 border-b py-5 sm:flex-row">
        <div className="grid flex-1 gap-1">
          <CardTitle>
            <Trans>Area Chart - Interactive</Trans>
          </CardTitle>
          <CardDescription>
            <Trans>Showing total visitors for the last 3 months</Trans>
          </CardDescription>
        </div>
        <SelectField
          name="area-interactive-range"
          items={timeRangeItems}
          value={timeRange}
          onValueChange={(value) => setTimeRange(value ?? "90d")}
          className="hidden w-40 sm:ml-auto sm:block"
        >
          <SelectTrigger aria-label={t`Select a value`}>
            <SelectValue />
          </SelectTrigger>
          <SelectContent>
            {timeRangeItems.map((item) => (
              <SelectItem key={item.value} value={item.value}>
                {item.label}
              </SelectItem>
            ))}
          </SelectContent>
        </SelectField>
      </CardHeader>
      <CardContent className="px-2 pt-4 sm:px-6 sm:pt-6">
        <ChartContainer config={chartConfig} className="aspect-auto h-[15.625rem] w-full">
          <AreaChart data={filteredData}>
            <defs>
              <linearGradient id="chart-area-interactive-desktop" x1="0" y1="0" x2="0" y2="1">
                <stop offset="5%" stopColor="var(--color-desktop)" stopOpacity={0.8} />
                <stop offset="95%" stopColor="var(--color-desktop)" stopOpacity={0.1} />
              </linearGradient>
              <linearGradient id="chart-area-interactive-mobile" x1="0" y1="0" x2="0" y2="1">
                <stop offset="5%" stopColor="var(--color-mobile)" stopOpacity={0.8} />
                <stop offset="95%" stopColor="var(--color-mobile)" stopOpacity={0.1} />
              </linearGradient>
            </defs>
            <CartesianGrid vertical={false} />
            <XAxis
              dataKey="date"
              tickLine={false}
              axisLine={false}
              tickMargin={8}
              minTickGap={32}
              tickFormatter={(value) => shortDateFormatter.format(new Date(value))}
            />
            <ChartTooltip
              cursor={false}
              content={
                <ChartTooltipContent
                  labelFormatter={(value) => shortDateFormatter.format(new Date(value))}
                  indicator="dot"
                />
              }
            />
            <Area
              dataKey="mobile"
              type="natural"
              fill="url(#chart-area-interactive-mobile)"
              stroke="var(--color-mobile)"
              stackId="a"
            />
            <Area
              dataKey="desktop"
              type="natural"
              fill="url(#chart-area-interactive-desktop)"
              stroke="var(--color-desktop)"
              stackId="a"
            />
            <ChartLegend content={<ChartLegendContent />} />
          </AreaChart>
        </ChartContainer>
      </CardContent>
    </Card>
  );
}
