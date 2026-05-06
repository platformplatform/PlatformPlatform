import { useState } from "react";

import { DashboardTrendPeriod } from "@/shared/lib/api/client";

import { DashboardHeader } from "./DashboardHeader";
import { DashboardKpiTiles } from "./DashboardKpiTiles";
import { DashboardMrrTrendCard } from "./DashboardMrrTrendCard";
import { DashboardPlanDistributionCard } from "./DashboardPlanDistributionCard";
import { DashboardRecentSignupsCard } from "./DashboardRecentSignupsCard";
import { DashboardRecentStripeEventsCard } from "./DashboardRecentStripeEventsCard";
import { DashboardTenantGrowthCard } from "./DashboardTenantGrowthCard";
import { DashboardUserLoginsCard } from "./DashboardUserLoginsCard";

export function DashboardSections() {
  const [period, setPeriod] = useState<DashboardTrendPeriod>(DashboardTrendPeriod.Last30Days);

  return (
    <div className="flex flex-col gap-6">
      <DashboardHeader period={period} onPeriodChange={setPeriod} />
      <DashboardKpiTiles period={period} />
      <div className="grid grid-cols-1 gap-6 lg:grid-cols-2">
        <DashboardMrrTrendCard period={period} />
        <DashboardPlanDistributionCard />
      </div>
      <div className="grid grid-cols-1 gap-6 lg:grid-cols-2">
        <DashboardTenantGrowthCard period={period} />
        <DashboardUserLoginsCard period={period} />
      </div>
      <div className="grid grid-cols-1 gap-6 lg:grid-cols-5">
        <div className="lg:col-span-2">
          <DashboardRecentSignupsCard />
        </div>
        <div className="lg:col-span-3">
          <DashboardRecentStripeEventsCard />
        </div>
      </div>
    </div>
  );
}
