import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { Card, CardContent, CardDescription, CardFooter, CardHeader, CardTitle } from "@repo/ui/components/Card";
import {
  type ChartConfig,
  ChartContainer,
  Label,
  PolarGrid,
  PolarRadiusAxis,
  RadialBar,
  RadialBarChart
} from "@repo/ui/components/Chart";
import { TrendingUpIcon } from "lucide-react";

const chartData = [{ browser: "safari", visitors: 1260, fill: "var(--color-safari)" }];

export function ChartRadialShapePreview() {
  const chartConfig = {
    visitors: { label: t`Visitors` },
    safari: { label: t`Safari`, color: "var(--chart-2)" }
  } satisfies ChartConfig;

  const visitorsLabel = t`Visitors`;

  return (
    <Card className="flex flex-col">
      <CardHeader className="items-center pb-0">
        <CardTitle>
          <Trans>Radial Chart - Shape</Trans>
        </CardTitle>
        <CardDescription>
          <Trans>January - June 2024</Trans>
        </CardDescription>
      </CardHeader>
      <CardContent className="flex-1 pb-0">
        <ChartContainer config={chartConfig} className="mx-auto aspect-square max-h-[15.625rem]">
          <RadialBarChart data={chartData} endAngle={100} innerRadius={65} outerRadius={95}>
            <PolarGrid
              gridType="circle"
              radialLines={false}
              stroke="none"
              className="first:fill-muted last:fill-background"
              polarRadius={[86, 74]}
            />
            <RadialBar dataKey="visitors" background={true} />
            <PolarRadiusAxis tick={false} tickLine={false} axisLine={false}>
              <Label
                content={({ viewBox }) => {
                  if (viewBox && "cx" in viewBox && "cy" in viewBox) {
                    return (
                      <text x={viewBox.cx} y={viewBox.cy} textAnchor="middle" dominantBaseline="middle">
                        <tspan x={viewBox.cx} y={viewBox.cy} className="fill-foreground text-4xl font-bold">
                          {new Intl.NumberFormat().format(chartData[0].visitors)}
                        </tspan>
                        <tspan x={viewBox.cx} y={(viewBox.cy || 0) + 24} className="fill-muted-foreground">
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
