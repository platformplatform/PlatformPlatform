import { t } from "@lingui/core/macro";
import { useLingui } from "@lingui/react";
import { Trans } from "@lingui/react/macro";
import { ToggleGroup, ToggleGroupItem } from "@repo/ui/components/ToggleGroup";

import { DashboardTrendPeriod } from "@/shared/lib/api/client";

interface DashboardHeaderProps {
  period: DashboardTrendPeriod;
  onPeriodChange: (period: DashboardTrendPeriod) => void;
}

export function DashboardHeader({ period, onPeriodChange }: Readonly<DashboardHeaderProps>) {
  const { i18n } = useLingui();
  const dateFormatter = new Intl.DateTimeFormat(i18n.locale, { weekday: "long", month: "long", day: "numeric" });
  const today = dateFormatter.format(new Date());

  return (
    <div className="flex flex-wrap items-end justify-between gap-3">
      <div>
        <h1>
          <Trans>Dashboard</Trans>
        </h1>
        <p className="mt-1 text-muted-foreground">
          <Trans>BackOffice overview · {today}</Trans>
        </p>
      </div>
      <ToggleGroup
        variant="outline"
        aria-label={t`Period`}
        value={[period]}
        onValueChange={(values) => {
          const next = values[0];
          if (next) {
            onPeriodChange(next as DashboardTrendPeriod);
          }
        }}
      >
        <ToggleGroupItem value={DashboardTrendPeriod.Last7Days} className="min-w-[3.5rem] justify-center">
          <Trans>7d</Trans>
        </ToggleGroupItem>
        <ToggleGroupItem value={DashboardTrendPeriod.Last30Days} className="min-w-[3.5rem] justify-center">
          <Trans>30d</Trans>
        </ToggleGroupItem>
        <ToggleGroupItem value={DashboardTrendPeriod.Last90Days} className="min-w-[3.5rem] justify-center">
          <Trans>90d</Trans>
        </ToggleGroupItem>
      </ToggleGroup>
    </div>
  );
}
