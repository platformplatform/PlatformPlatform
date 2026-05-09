import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { Card, CardContent, CardDescription, CardFooter, CardHeader, CardTitle } from "@repo/ui/components/Card";
import {
  type ChartConfig,
  ChartContainer,
  ChartTooltip,
  ChartTooltipContent,
  Label,
  PolarRadiusAxis,
  RadialBar,
  RadialBarChart
} from "@repo/ui/components/Chart";
import { TrendingUpIcon } from "lucide-react";

const chartData = [{ month: "january", mobile: 570, desktop: 1260 }];

export function ChartRadialStackedPreview() {
  const chartConfig = {
    desktop: { label: t`Desktop`, color: "var(--chart-1)" },
    mobile: { label: t`Mobile`, color: "var(--chart-2)" }
  } satisfies ChartConfig;

  const totalVisitors = chartData[0].desktop + chartData[0].mobile;
  const visitorsLabel = t`Visitors`;

  return (
    <Card className="flex flex-col">
      <CardHeader className="items-center pb-0">
        <CardTitle>
          <Trans>Radial Chart - Stacked</Trans>
        </CardTitle>
        <CardDescription>
          <Trans>January - June 2024</Trans>
        </CardDescription>
      </CardHeader>
      <CardContent className="flex flex-1 items-center pb-0">
        <ChartContainer config={chartConfig} className="mx-auto aspect-square w-full max-w-[15.625rem]">
          <RadialBarChart data={chartData} endAngle={180} innerRadius={80} outerRadius={110}>
            <RadialBar
              dataKey="mobile"
              fill="var(--color-mobile)"
              stackId="a"
              cornerRadius={5}
              className="stroke-transparent stroke-2"
            />
            <RadialBar
              dataKey="desktop"
              stackId="a"
              cornerRadius={5}
              fill="var(--color-desktop)"
              className="stroke-transparent stroke-2"
            />
            <ChartTooltip cursor={false} content={<ChartTooltipContent hideLabel={true} />} />
            <PolarRadiusAxis tick={false} tickLine={false} axisLine={false}>
              <Label
                content={({ viewBox }) => {
                  if (viewBox && "cx" in viewBox && "cy" in viewBox) {
                    return (
                      <text x={viewBox.cx} y={viewBox.cy} textAnchor="middle">
                        <tspan x={viewBox.cx} y={(viewBox.cy || 0) - 16} className="fill-foreground text-2xl font-bold">
                          {new Intl.NumberFormat().format(totalVisitors)}
                        </tspan>
                        <tspan x={viewBox.cx} y={(viewBox.cy || 0) + 4} className="fill-muted-foreground">
                          {visitorsLabel}
                        </tspan>
                      </text>
                    );
                  }
                }}
              />
            </PolarRadiusAxis>
          </RadialBarChart>
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
