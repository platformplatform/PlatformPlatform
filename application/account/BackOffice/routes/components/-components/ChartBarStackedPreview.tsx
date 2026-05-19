import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { Card, CardContent, CardDescription, CardFooter, CardHeader, CardTitle } from "@repo/ui/components/Card";
import {
  Bar,
  BarChart,
  CartesianGrid,
  type ChartConfig,
  ChartContainer,
  ChartLegend,
  ChartLegendContent,
  ChartTooltip,
  ChartTooltipContent,
  XAxis
} from "@repo/ui/components/Chart";
import { TrendingUpIcon } from "lucide-react";

const chartData = [
  { month: "January", desktop: 186, mobile: 80 },
  { month: "February", desktop: 305, mobile: 200 },
  { month: "March", desktop: 237, mobile: 120 },
  { month: "April", desktop: 73, mobile: 190 },
  { month: "May", desktop: 209, mobile: 130 },
  { month: "June", desktop: 214, mobile: 140 }
];

export function ChartBarStackedPreview() {
  const chartConfig = {
    desktop: { label: t`Desktop`, color: "var(--chart-1)" },
    mobile: { label: t`Mobile`, color: "var(--chart-2)" }
  } satisfies ChartConfig;

  return (
    <Card>
      <CardHeader>
        <CardTitle>
          <Trans>Bar Chart - Stacked + Legend</Trans>
        </CardTitle>
        <CardDescription>
          <Trans>January - June 2024</Trans>
        </CardDescription>
      </CardHeader>
      <CardContent>
        <ChartContainer config={chartConfig}>
          <BarChart accessibilityLayer={true} data={chartData}>
            <CartesianGrid vertical={false} />
            <XAxis
              dataKey="month"
              tickLine={false}
              tickMargin={10}
              axisLine={false}
              tickFormatter={(value) => value.slice(0, 3)}
            />
            <ChartTooltip content={<ChartTooltipContent hideLabel={true} />} />
            <ChartLegend content={<ChartLegendContent />} />
            <Bar dataKey="desktop" stackId="a" fill="var(--color-desktop)" radius={[0, 0, 4, 4]} />
            <Bar dataKey="mobile" stackId="a" fill="var(--color-mobile)" radius={[4, 4, 0, 0]} />
          </BarChart>
        </ChartContainer>
      </CardContent>
      <CardFooter className="flex-col items-start gap-2 text-sm">
        <div className="flex gap-2 leading-none font-medium">
          <Trans>Trending up by 5.2% this month</Trans> <TrendingUpIcon className="size-4" />
        </div>
        <div className="leading-none text-muted-foreground">
          <Trans>Showing total visitors for the last 6 months</Trans>
        </div>
      </CardFooter>
    </Card>
  );
}
