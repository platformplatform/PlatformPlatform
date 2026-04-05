import { t } from "@lingui/core/macro";
import { AppLayout } from "@repo/ui/components/AppLayout";
import { SidebarInset, SidebarProvider } from "@repo/ui/components/Sidebar";
import { Skeleton } from "@repo/ui/components/Skeleton";
import { createFileRoute, Link } from "@tanstack/react-router";
import { ArrowLeftIcon } from "lucide-react";

import { BackOfficeSideMenu } from "@/shared/components/BackOfficeSideMenu";
import { api } from "@/shared/lib/api/client";

import type { GetFeatureFlagsResponse, GetFeatureFlagTenantsResponse } from "./-components/types";

import { FeatureFlagInfoSection } from "./-components/FeatureFlagInfoSection";
import { getFeatureFlagDescription, getFeatureFlagName } from "./-components/flagLabels";
import { PlanFeatureFlagInfoSection, PlanFeatureFlagTenantsSection } from "./-components/PlanFeatureFlagSections";
import { ScopeIcon } from "./-components/ScopeIcon";
import { TenantOverridesSection } from "./-components/TenantOverridesSection";
import { UserOverridesSection } from "./-components/UserOverridesSection";

export const Route = createFileRoute("/feature-flags/$flagKey")({
  staticData: { trackingTitle: "Feature flag detail" },
  component: FeatureFlagDetailPage
});

export default function FeatureFlagDetailPage() {
  const { flagKey } = Route.useParams();

  const { data: featureFlagsData, isLoading: isLoadingFeatureFlags } = api.useQuery(
    "get",
    "/api/back-office/feature-flags"
  ) as {
    data: GetFeatureFlagsResponse | undefined;
    isLoading: boolean;
  };

  const featureFlag = featureFlagsData?.flags?.find((f) => f.key === flagKey);

  const { data: tenantsData, isLoading: isLoadingTenants } = api.useQuery(
    "get",
    "/api/back-office/feature-flags/{flagKey}/tenants",
    { params: { path: { flagKey } } },
    { enabled: featureFlag?.scope === "Tenant" }
  ) as {
    data: GetFeatureFlagTenantsResponse | undefined;
    isLoading: boolean;
  };

  const isPlanFeatureFlag = featureFlag?.requiredPlan != null;
  const isLoading = isLoadingFeatureFlags || (featureFlag?.scope === "Tenant" && isLoadingTenants);
  const featureFlagName = featureFlag ? getFeatureFlagName(featureFlag.key) : flagKey;
  const description = featureFlag ? getFeatureFlagDescription(featureFlag.key) || featureFlag.description : "";

  return (
    <SidebarProvider>
      <BackOfficeSideMenu />
      <SidebarInset>
        <AppLayout
          maxWidth="64rem"
          browserTitle={featureFlagName}
          title={
            <div className="flex items-center gap-3">
              <Link to="/feature-flags" className="text-muted-foreground hover:text-foreground">
                <ArrowLeftIcon className="size-5" aria-label={t`Back to feature flags`} />
              </Link>
              {featureFlag && <ScopeIcon scope={featureFlag.scope} className="size-6 stroke-[2.5] text-foreground" />}
              <span>{featureFlagName}</span>
            </div>
          }
          subtitle={featureFlag ? description : undefined}
        >
          {isLoading ? (
            <FeatureFlagDetailSkeleton />
          ) : featureFlag ? (
            <div className="flex flex-col gap-8">
              {isPlanFeatureFlag ? (
                <PlanFeatureFlagInfoSection featureFlag={featureFlag} />
              ) : (
                <FeatureFlagInfoSection featureFlag={featureFlag} />
              )}
              {featureFlag.scope === "Tenant" && !isPlanFeatureFlag && (
                <TenantOverridesSection
                  flagKey={featureFlag.key}
                  featureFlagDescription={featureFlagName}
                  tenants={tenantsData?.tenants ?? []}
                  showRolloutBucket={featureFlag.isAbTestEligible}
                  rolloutBucketRange={
                    featureFlag.bucketStart != null && featureFlag.bucketEnd != null
                      ? { bucketStart: featureFlag.bucketStart, bucketEnd: featureFlag.bucketEnd }
                      : null
                  }
                  isFeatureFlagActive={featureFlag.isActive}
                />
              )}
              {featureFlag.scope === "Tenant" && isPlanFeatureFlag && (
                <PlanFeatureFlagTenantsSection tenants={tenantsData?.tenants ?? []} />
              )}
              {featureFlag.scope === "User" && (
                <UserOverridesSection
                  flagKey={featureFlag.key}
                  featureFlagDescription={featureFlagName}
                  showRolloutBucket={featureFlag.isAbTestEligible}
                  rolloutBucketRange={
                    featureFlag.bucketStart != null && featureFlag.bucketEnd != null
                      ? { bucketStart: featureFlag.bucketStart, bucketEnd: featureFlag.bucketEnd }
                      : null
                  }
                  isFeatureFlagActive={featureFlag.isActive}
                />
              )}
            </div>
          ) : null}
        </AppLayout>
      </SidebarInset>
    </SidebarProvider>
  );
}

function FeatureFlagDetailSkeleton() {
  return (
    <div className="flex flex-col gap-8">
      <Skeleton className="h-20 w-full rounded-lg" />
      <div className="flex flex-col gap-4">
        <Skeleton className="h-6 w-40" />
        <Skeleton className="h-10 w-full rounded-md" />
        <Skeleton className="h-14 w-full rounded-md" />
        <Skeleton className="h-14 w-full rounded-md" />
      </div>
    </div>
  );
}
