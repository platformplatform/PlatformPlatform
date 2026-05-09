import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { Card, CardContent, CardDescription, CardFooter, CardHeader, CardTitle } from "@repo/ui/components/Card";
import {
  type ChartConfig,
  ChartContainer,
  ChartTooltip,
  ChartTooltipContent,
  Label,
  Pie,
  PieChart
} from "@repo/ui/components/Chart";
import { TrendingUpIcon } from "lucide-react";
import { useMemo } from "react";

const chartData = [
  { browser: "chrome", visitors: 275, fill: "var(--color-chrome)" },
  { browser: "safari", visitors: 200, fill: "var(--color-safari)" },
  { browser: "firefox", visitors: 287, fill: "var(--color-firefox)" },
  { browser: "edge", visitors: 173, fill: "var(--color-edge)" },
  { browser: "other", visitors: 190, fill: "var(--color-other)" }
];

export function ChartPieDonutTextPreview() {
  const chartConfig = {
    visitors: { label: t`Visitors` },
    chrome: { label: t`Chrome`, color: "var(--chart-1)" },
    safari: { label: t`Safari`, color: "var(--chart-2)" },
    firefox: { label: t`Firefox`, color: "var(--chart-3)" },
    edge: { label: t`Edge`, color: "var(--chart-4)" },
    other: { label: t`Other`, color: "var(--chart-5)" }
  } satisfies ChartConfig;

  const totalVisitors = useMemo(
    () => chartData.reduce((accumulator, current) => accumulator + current.visitors, 0),
    []
  );
  const visitorsLabel = t`Visitors`;

  return (
    <Card className="flex flex-col">
      <CardHeader className="items-center pb-0">
        <CardTitle>
          <Trans>Pie Chart - Donut with Text</Trans>
        </CardTitle>
        <CardDescription>
          <Trans>January - June 2024</Trans>
        </CardDescription>
      </CardHeader>
      <CardContent className="flex-1 pb-0">
        <ChartContainer config={chartConfig} className="mx-auto aspect-square max-h-[15.625rem]">
          <PieChart>
            <ChartTooltip cursor={false} content={<ChartTooltipContent hideLabel={true} />} />
            <Pie data={chartData} dataKey="visitors" nameKey="browser" innerRadius={60} strokeWidth={5}>
              <Label
                content={({ viewBox }) => {
                  if (viewBox && "cx" in viewBox && "cy" in viewBox) {
                    return (
                      <text x={viewBox.cx} y={viewBox.cy} textAnchor="middle" dominantBaseline="middle">
                        <tspan x={viewBox.cx} y={viewBox.cy} className="fill-foreground text-3xl font-bold">
                          {new Intl.NumberFormat().format(totalVisitors)}
                        </tspan>
                        <tspan x={viewBox.cx} y={(viewBox.cy || 0) + 24} className="fill-muted-foreground">
                          {visitorsLabel}
                        </tspan>
                      </text>
                    );
                  }
                }}
              />
            </Pie>
          </PieChart>
        </ChartContainer>
      </CardContent>
      <CardFooter className="flex-col gap-2 text-sm">
        <div className="flex items-center gap-2 leading-none font-medium">
          <Trans>Trending up by 5.2% this month</Trans> <TrendingUpIcon className="size-4" />
        </div>
        <div className="leading-none text-muted-foreground">
          <Trans>Showing total visitors for the last 6 months</Trans>
        </div>
      </CardFooter>
    </Card>
  );
}
