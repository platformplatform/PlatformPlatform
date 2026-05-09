import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { Button } from "@repo/ui/components/Button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@repo/ui/components/Card";
import {
  Bar,
  BarChart,
  CartesianGrid,
  type ChartConfig,
  ChartContainer,
  ChartTooltip,
  ChartTooltipContent,
  XAxis
} from "@repo/ui/components/Chart";
import { useMemo, useState } from "react";

import { dailyVisitors as chartData } from "./chartSampleData";

const shortDateFormatter = new Intl.DateTimeFormat("en-US", { month: "short", day: "numeric" });
const fullDateFormatter = new Intl.DateTimeFormat("en-US", { month: "short", day: "numeric", year: "numeric" });

type SeriesKey = "desktop" | "mobile";

export function ChartBarInteractivePreview() {
  const [activeChart, setActiveChart] = useState<SeriesKey>("desktop");

  const chartConfig = {
    views: { label: t`Page Views` },
    desktop: { label: t`Desktop`, color: "var(--chart-2)" },
    mobile: { label: t`Mobile`, color: "var(--chart-1)" }
  } satisfies ChartConfig;

  const total = useMemo(
    () => ({
      desktop: chartData.reduce((accumulator, current) => accumulator + current.desktop, 0),
      mobile: chartData.reduce((accumulator, current) => accumulator + current.mobile, 0)
    }),
    []
  );

  const series: SeriesKey[] = ["desktop", "mobile"];

  return (
    <Card className="py-0">
      <CardHeader className="flex flex-col items-stretch border-b p-0 sm:flex-row">
        <div className="flex flex-1 flex-col justify-center gap-1 px-6 pt-4 pb-3 sm:py-0">
          <CardTitle>
            <Trans>Bar Chart - Interactive</Trans>
          </CardTitle>
          <CardDescription>
            <Trans>Showing total visitors for the last 3 months</Trans>
          </CardDescription>
        </div>
        <div className="flex">
          {series.map((key) => (
            <Button
              key={key}
              variant="ghost"
              data-active={activeChart === key}
              onClick={() => setActiveChart(key)}
              className="relative z-30 flex h-auto flex-1 flex-col items-start justify-center gap-1 rounded-none border-t px-6 py-4 text-left even:border-l data-[active=true]:bg-muted/50 sm:min-w-[12.5rem] sm:border-t-0 sm:border-l sm:px-8 sm:py-6"
            >
              <span className="text-xs text-muted-foreground">{chartConfig[key].label}</span>
              <span className="text-lg leading-none font-bold sm:text-3xl">
                {new Intl.NumberFormat().format(total[key])}
              </span>
            </Button>
          ))}
        </div>
      </CardHeader>
      <CardContent className="px-2 sm:p-6">
        <ChartContainer config={chartConfig} className="aspect-auto h-[15.625rem] w-full">
          <BarChart accessibilityLayer={true} data={chartData} margin={{ left: 12, right: 12 }}>
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
              content={
                <ChartTooltipContent
                  className="w-[9.375rem]"
                  nameKey="views"
                  labelFormatter={(value) => fullDateFormatter.format(new Date(value))}
                />
              }
            />
            <Bar dataKey={activeChart} fill={`var(--color-${activeChart})`} />
          </BarChart>
        </ChartContainer>
      </CardContent>
    </Card>
  );
}
