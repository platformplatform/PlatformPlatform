import { t } from "@lingui/core/macro";
import { AppLayout } from "@repo/ui/components/AppLayout";
import { SidebarInset, SidebarProvider } from "@repo/ui/components/Sidebar";
import { Skeleton } from "@repo/ui/components/Skeleton";
import { createFileRoute, Link } from "@tanstack/react-router";
import { ArrowLeftIcon } from "lucide-react";
import { z } from "zod";

import { BackOfficeSideMenu } from "@/shared/components/BackOfficeSideMenu";
import { api, FeatureFlagAudienceState, SubscriptionPlan, UserRole } from "@/shared/lib/api/client";

import type { GetFeatureFlagsResponse } from "./-components/types";

import { FeatureFlagInfoSection } from "./-components/FeatureFlagInfoSection";
import { getFeatureFlagDescription, getFeatureFlagName } from "./-components/flagLabels";
import { PlanFeatureFlagInfoSection, PlanFeatureFlagTenantsSection } from "./-components/PlanFeatureFlagSections";
import { ScopeIcon } from "./-components/ScopeIcon";
import { ALL_STATE_FILTER } from "./-components/stateFilter";
import { TenantOverridesSection } from "./-components/TenantOverridesSection";
import { UserOverridesSection } from "./-components/UserOverridesSection";

const stateFilterSchema = z.enum([
  FeatureFlagAudienceState.Enabled,
  FeatureFlagAudienceState.Disabled,
  ALL_STATE_FILTER
]);

const flagKeySearchSchema = z.object({
  tenantsSearch: z.string().optional(),
  tenantsPlans: z.array(z.nativeEnum(SubscriptionPlan)).max(10).optional(),
  tenantsState: stateFilterSchema.optional(),
  tenantsHasOverride: z.boolean().optional(),
  tenantsPageOffset: z.number().int().nonnegative().optional(),
  usersSearch: z.string().optional(),
  usersRoles: z.array(z.nativeEnum(UserRole)).max(10).optional(),
  usersState: stateFilterSchema.optional(),
  usersHasOverride: z.boolean().optional(),
  usersPageOffset: z.number().int().nonnegative().optional()
});

export const Route = createFileRoute("/feature-flags/$flagKey")({
  staticData: { trackingTitle: "Feature flag detail" },
  validateSearch: flagKeySearchSchema,
  component: FeatureFlagDetailPage
});

export default function FeatureFlagDetailPage() {
  const { flagKey } = Route.useParams();
  const {
    tenantsSearch,
    tenantsPlans,
    tenantsState,
    tenantsHasOverride,
    tenantsPageOffset,
    usersSearch,
    usersRoles,
    usersState,
    usersHasOverride,
    usersPageOffset
  } = Route.useSearch();

  const { data: featureFlagsData, isLoading: isLoadingFeatureFlags } = api.useQuery(
    "get",
    "/api/back-office/feature-flags"
  ) as {
    data: GetFeatureFlagsResponse | undefined;
    isLoading: boolean;
  };

  const featureFlag = featureFlagsData?.flags?.find((f) => f.key === flagKey);

  const isPlanFeatureFlag = featureFlag?.requiredPlan != null;
  const isLoading = isLoadingFeatureFlags;
  const featureFlagName = featureFlag ? getFeatureFlagName(featureFlag.key) : flagKey;
  const description = featureFlag ? getFeatureFlagDescription(featureFlag.key) || featureFlag.description : "";

  return (
    <SidebarProvider>
      <BackOfficeSideMenu />
      <SidebarInset>
        <AppLayout
          variant="center"
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
                  showRolloutBucket={featureFlag.isAbTestEligible}
                  isFeatureFlagActive={featureFlag.isActive}
                  search={tenantsSearch}
                  plans={tenantsPlans ?? []}
                  state={tenantsState}
                  hasOverride={tenantsHasOverride ?? false}
                  pageOffset={tenantsPageOffset}
                />
              )}
              {featureFlag.scope === "Tenant" && isPlanFeatureFlag && (
                <PlanFeatureFlagTenantsSection flagKey={featureFlag.key} />
              )}
              {featureFlag.scope === "User" && (
                <UserOverridesSection
                  flagKey={featureFlag.key}
                  featureFlagDescription={featureFlagName}
                  showRolloutBucket={featureFlag.isAbTestEligible}
                  isFeatureFlagActive={featureFlag.isActive}
                  search={usersSearch}
                  roles={usersRoles ?? []}
                  state={usersState}
                  hasOverride={usersHasOverride ?? false}
                  pageOffset={usersPageOffset}
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
