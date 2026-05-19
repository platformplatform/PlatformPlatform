import { Trans } from "@lingui/react/macro";
import { Link } from "@repo/ui/components/Link";
import { ArrowUpRightIcon } from "lucide-react";

import { ChartAreaInteractivePreview } from "./ChartAreaInteractivePreview";
import { ChartBarInteractivePreview } from "./ChartBarInteractivePreview";
import { ChartBarStackedPreview } from "./ChartBarStackedPreview";
import { ChartPieDonutTextPreview } from "./ChartPieDonutTextPreview";
import { ChartRadialShapePreview } from "./ChartRadialShapePreview";
import { ChartRadialStackedPreview } from "./ChartRadialStackedPreview";

export function ChartsPreview() {
  return (
    <div className="flex flex-col gap-6">
      <div className="flex items-center justify-between">
        <p className="text-sm text-muted-foreground">
          <Trans>A small selection of chart variants built on recharts.</Trans>
        </p>
        <Link href="https://ui.shadcn.com/charts/area#charts" target="_blank" rel="noreferrer" className="gap-1">
          <Trans>See more variations on ShadCN</Trans>
          <ArrowUpRightIcon className="size-4" />
        </Link>
      </div>
      <ChartAreaInteractivePreview />
      <ChartBarInteractivePreview />
      <div className="grid gap-6 lg:grid-cols-2">
        <ChartBarStackedPreview />
        <ChartPieDonutTextPreview />
        <ChartRadialStackedPreview />
        <ChartRadialShapePreview />
      </div>
    </div>
  );
}
