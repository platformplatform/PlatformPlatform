import { t } from "@lingui/core/macro";
import { AppLayout } from "@repo/ui/components/AppLayout";
import { Skeleton } from "@repo/ui/components/Skeleton";
import { createFileRoute, Link } from "@tanstack/react-router";
import { ArrowLeftIcon } from "lucide-react";

import { api } from "@/shared/lib/api/client";

import type { GetFeatureFlagsResponse, GetFlagTenantsResponse } from "./-components/types";

import { FlagInfoSection } from "./-components/FlagInfoSection";
import { getFlagDescription, getFlagName } from "./-components/flagLabels";
import { PlanFlagInfoSection, PlanFlagTenantsSection } from "./-components/PlanFlagSections";
import { ScopeIcon } from "./-components/ScopeIcon";
import { TenantOverridesSection } from "./-components/TenantOverridesSection";
import { UserOverridesSection } from "./-components/UserOverridesSection";

export const Route = createFileRoute("/back-office/feature-flags/$flagKey")({
  staticData: { trackingTitle: "Feature flag detail" },
  component: FlagDetailPage
});

export default function FlagDetailPage() {
  const { flagKey } = Route.useParams();

  const { data: flagsData, isLoading: isLoadingFlags } = api.useQuery("get", "/api/back-office/feature-flags") as {
    data: GetFeatureFlagsResponse | undefined;
    isLoading: boolean;
  };

  const flag = flagsData?.flags?.find((f) => f.key === flagKey);

  const { data: tenantsData, isLoading: isLoadingTenants } = api.useQuery(
    "get",
    "/api/back-office/feature-flags/{flagKey}/tenants",
    { params: { path: { flagKey } } },
    { enabled: flag?.scope === "Tenant" }
  ) as {
    data: GetFlagTenantsResponse | undefined;
    isLoading: boolean;
  };

  const isPlanFlag = flag?.requiredPlan != null;
  const isLoading = isLoadingFlags || (flag?.scope === "Tenant" && isLoadingTenants);
  const flagName = flag ? getFlagName(flag.key) : flagKey;
  const description = flag ? getFlagDescription(flag.key) || flag.description : "";

  return (
    <AppLayout
      maxWidth="64rem"
      browserTitle={flagName}
      title={
        <div className="flex items-center gap-3">
          <Link to="/back-office/feature-flags" className="text-muted-foreground hover:text-foreground">
            <ArrowLeftIcon className="size-5" aria-label={t`Back to feature flags`} />
          </Link>
          {flag && <ScopeIcon scope={flag.scope} className="size-6 stroke-[2.5] text-foreground" />}
          <span>{flagName}</span>
        </div>
      }
      subtitle={flag ? description : undefined}
    >
      {isLoading ? (
        <FlagDetailSkeleton />
      ) : flag ? (
        <div className="flex flex-col gap-8">
          {isPlanFlag ? <PlanFlagInfoSection flag={flag} /> : <FlagInfoSection flag={flag} />}
          {flag.scope === "Tenant" && !isPlanFlag && (
            <TenantOverridesSection
              flagKey={flag.key}
              flagDescription={flagName}
              tenants={tenantsData?.tenants ?? []}
              showBucket={flag.isAbTestEligible}
              bucketRange={
                flag.bucketStart != null && flag.bucketEnd != null
                  ? { bucketStart: flag.bucketStart, bucketEnd: flag.bucketEnd }
                  : null
              }
              isFlagActive={flag.isActive}
            />
          )}
          {flag.scope === "Tenant" && isPlanFlag && <PlanFlagTenantsSection tenants={tenantsData?.tenants ?? []} />}
          {flag.scope === "User" && (
            <UserOverridesSection
              flagKey={flag.key}
              flagDescription={flagName}
              showBucket={flag.isAbTestEligible}
              bucketRange={
                flag.bucketStart != null && flag.bucketEnd != null
                  ? { bucketStart: flag.bucketStart, bucketEnd: flag.bucketEnd }
                  : null
              }
              isFlagActive={flag.isActive}
            />
          )}
        </div>
      ) : null}
    </AppLayout>
  );
}

function FlagDetailSkeleton() {
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
