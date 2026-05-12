import { useState } from "react";

import { DashboardTrendPeriod } from "@/shared/lib/api/client";

import { DashboardHeader } from "./DashboardHeader";
import { DashboardKpiTiles } from "./DashboardKpiTiles";
import { DashboardMrrTrendCard } from "./DashboardMrrTrendCard";
import { DashboardPlanDistributionCard } from "./DashboardPlanDistributionCard";
import { DashboardRecentLoginsCard } from "./DashboardRecentLoginsCard";
import { DashboardRecentPaymentsCard } from "./DashboardRecentPaymentsCard";
import { DashboardRecentSignupsCard } from "./DashboardRecentSignupsCard";
import { DashboardRecentStripeEventsCard } from "./DashboardRecentStripeEventsCard";
import { DashboardRevenueTrendCard } from "./DashboardRevenueTrendCard";
import { DashboardTenantGrowthCard } from "./DashboardTenantGrowthCard";
import { DashboardUserLoginsCard } from "./DashboardUserLoginsCard";

const isSubscriptionEnabled = import.meta.runtime_env.PUBLIC_SUBSCRIPTION_ENABLED === "true";

export function DashboardSections() {
  const [period, setPeriod] = useState<DashboardTrendPeriod>(DashboardTrendPeriod.Last30Days);

  return (
    <div className="flex flex-col gap-4">
      <DashboardHeader period={period} onPeriodChange={setPeriod} />
      <DashboardKpiTiles period={period} />
      {isSubscriptionEnabled && (
        <div className="grid grid-cols-1 gap-4 lg:grid-cols-2">
          <DashboardMrrTrendCard period={period} />
          <DashboardPlanDistributionCard />
        </div>
      )}
      {isSubscriptionEnabled && <DashboardRevenueTrendCard period={period} />}
      <div className="grid grid-cols-1 gap-4 lg:grid-cols-2">
        <DashboardTenantGrowthCard period={period} />
        <DashboardUserLoginsCard period={period} />
      </div>
      <div className="grid grid-cols-1 gap-4 lg:grid-cols-2">
        <DashboardRecentSignupsCard />
        <DashboardRecentLoginsCard />
      </div>
      {isSubscriptionEnabled && (
        <div className="grid grid-cols-1 gap-4 lg:grid-cols-2">
          <DashboardRecentPaymentsCard />
          <DashboardRecentStripeEventsCard />
        </div>
      )}
    </div>
  );
}
