import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { Card } from "@repo/ui/components/Card";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@repo/ui/components/Select";
import { ToggleGroup, ToggleGroupItem } from "@repo/ui/components/ToggleGroup";
import { TrendingUpIcon } from "lucide-react";
import { useState } from "react";

import { DashboardTrendMetric, DashboardTrendPeriod } from "@/shared/lib/api/client";

import { DashboardTrendChart } from "./DashboardTrendChart";

export function DashboardTrendsSection() {
  const [metric, setMetric] = useState<DashboardTrendMetric>(DashboardTrendMetric.NewTenants);
  const [period, setPeriod] = useState<DashboardTrendPeriod>(DashboardTrendPeriod.Last30Days);

  return (
    <section className="flex flex-col gap-3">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <h4 className="flex items-center gap-2">
          <TrendingUpIcon className="size-4 text-muted-foreground" aria-hidden="true" />
          <Trans>Trends</Trans>
        </h4>
        <div className="flex flex-wrap items-center gap-3">
          <Select value={metric} onValueChange={(value) => value && setMetric(value as DashboardTrendMetric)}>
            <SelectTrigger aria-label={t`Metric`} className="w-[12rem]">
              <SelectValue />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value={DashboardTrendMetric.NewTenants}>
                <Trans>New tenants</Trans>
              </SelectItem>
              <SelectItem value={DashboardTrendMetric.NewUsers}>
                <Trans>New users</Trans>
              </SelectItem>
              <SelectItem value={DashboardTrendMetric.LoginActivity}>
                <Trans>Login activity</Trans>
              </SelectItem>
            </SelectContent>
          </Select>

          <ToggleGroup
            variant="outline"
            aria-label={t`Period`}
            value={[period]}
            onValueChange={(values) => {
              const next = values[0];
              if (next) {
                setPeriod(next as DashboardTrendPeriod);
              }
            }}
          >
            <ToggleGroupItem value={DashboardTrendPeriod.Last7Days} className="min-w-[5rem] justify-center">
              <Trans>7 days</Trans>
            </ToggleGroupItem>
            <ToggleGroupItem value={DashboardTrendPeriod.Last30Days} className="min-w-[5rem] justify-center">
              <Trans>30 days</Trans>
            </ToggleGroupItem>
            <ToggleGroupItem value={DashboardTrendPeriod.Last90Days} className="min-w-[5rem] justify-center">
              <Trans>90 days</Trans>
            </ToggleGroupItem>
          </ToggleGroup>
        </div>
      </div>

      <Card className="rounded-lg p-4 shadow-none">
        <DashboardTrendChart metric={metric} period={period} />
      </Card>
    </section>
  );
}
