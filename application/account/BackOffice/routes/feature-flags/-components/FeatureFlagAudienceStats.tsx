import { api } from "@/shared/lib/api/client";

import type { FeatureFlagScope } from "./types";

import { OverrideStats } from "./OverrideStats";

interface FeatureFlagAudienceStatsProps {
  flagKey: string;
  scope: FeatureFlagScope;
  showOverride: boolean;
}

// Header-level audience stats. Fetched without filters so the numbers stay stable as users change
// the toolbar selection below the header — the chips describe "the flag's audience" rather than
// "the current view".
export function FeatureFlagAudienceStats({ flagKey, scope, showOverride }: Readonly<FeatureFlagAudienceStatsProps>) {
  const isTenantScope = scope === "Tenant";

  const tenantStats = api.useQuery(
    "get",
    "/api/back-office/feature-flags/{flagKey}/tenants",
    { params: { path: { flagKey }, query: { PageSize: 1 } } },
    { enabled: isTenantScope }
  );

  const userStats = api.useQuery(
    "get",
    "/api/back-office/feature-flags/{flagKey}/users",
    { params: { path: { flagKey }, query: { PageSize: 1 } } },
    { enabled: !isTenantScope }
  );

  const data = isTenantScope ? tenantStats.data : userStats.data;
  // Reserve the chip row height while loading so the page does not grow when the query resolves
  // (otherwise the user sees the page suddenly extend after a moment and a new scroll range appears).
  if (data === undefined) return <div className="h-[2.125rem]" aria-hidden={true} />;

  const populationCount = data.enabledCount + data.disabledCount;
  if (populationCount === 0) return <div className="h-[2.125rem]" aria-hidden={true} />;

  return (
    <OverrideStats
      total={populationCount}
      enabled={data.enabledCount}
      disabled={data.disabledCount}
      override={data.overrideCount}
      showOverride={showOverride}
    />
  );
}
