import { Trans } from "@lingui/react/macro";
import { useUserInfo } from "@repo/infrastructure/auth/hooks";
import { AppLayout } from "@repo/ui/components/AppLayout";
import { Button } from "@repo/ui/components/Button";
import { SidebarInset, SidebarProvider } from "@repo/ui/components/Sidebar";
import { getFeatureFlagDescription, getFeatureFlagName } from "@repo/ui/featureFlags/labels";
import { createFileRoute } from "@tanstack/react-router";
import { Trash2Icon } from "lucide-react";
import { useState } from "react";
import { z } from "zod";

import { BackOfficeSideMenu } from "@/shared/components/BackOfficeSideMenu";
import {
  api,
  FeatureFlagAudienceState,
  SortableFeatureFlagTenantProperties,
  SortableFeatureFlagUserProperties,
  SortOrder,
  SubscriptionPlan,
  UserRole
} from "@/shared/lib/api/client";

import type { GetFeatureFlagsResponse } from "./-components/types";

import { DeleteFeatureFlagDialog } from "./-components/DeleteFeatureFlagDialog";
import { FeatureFlagDetailSkeleton } from "./-components/FeatureFlagDetailSkeleton";
import { FeatureFlagDetailTitle } from "./-components/FeatureFlagDetailTitle";
import { FeatureFlagInfoSection } from "./-components/FeatureFlagInfoSection";
import { PlanFeatureFlagInfoSection, PlanFeatureFlagTenantsSection } from "./-components/PlanFeatureFlagSections";
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
  tenantsOrderBy: z.nativeEnum(SortableFeatureFlagTenantProperties).optional(),
  tenantsSortOrder: z.nativeEnum(SortOrder).optional(),
  usersSearch: z.string().optional(),
  usersRoles: z.array(z.nativeEnum(UserRole)).max(10).optional(),
  usersState: stateFilterSchema.optional(),
  usersHasOverride: z.boolean().optional(),
  usersPageOffset: z.number().int().nonnegative().optional(),
  usersOrderBy: z.nativeEnum(SortableFeatureFlagUserProperties).optional(),
  usersSortOrder: z.nativeEnum(SortOrder).optional()
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
    tenantsOrderBy,
    tenantsSortOrder,
    usersSearch,
    usersRoles,
    usersState,
    usersHasOverride,
    usersPageOffset,
    usersOrderBy,
    usersSortOrder
  } = Route.useSearch();

  const [isDeleteDialogOpen, setIsDeleteDialogOpen] = useState(false);

  // Activate/Deactivate and Delete server-side endpoints require AdminPolicyName (admin group
  // membership), so the matching controls are disabled for non-admin back-office users. UserInfo.role
  // mirrors the back-office groups claim — null when the principal has no group, set otherwise.
  const userInfo = useUserInfo();
  const canActivate = userInfo?.role != null;

  const { data: featureFlagsData, isLoading: isLoadingFeatureFlags } = api.useQuery(
    "get",
    "/api/back-office/feature-flags",
    { params: { query: { IncludeDeleted: true } } }
  ) as {
    data: GetFeatureFlagsResponse | undefined;
    isLoading: boolean;
  };

  const featureFlag = featureFlagsData?.flags?.find((f) => f.key === flagKey);
  const isDeleted = featureFlag?.deletedAt != null;
  const isOrphaned = featureFlag?.orphanedAt != null && !isDeleted;

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
          title={<FeatureFlagDetailTitle featureFlag={featureFlag} featureFlagName={featureFlagName} />}
          subtitle={featureFlag ? description : undefined}
        >
          {isLoading ? (
            <FeatureFlagDetailSkeleton />
          ) : featureFlag ? (
            // min-h-0 + flex-1 lets the trailing list section claim the remaining vertical space so
            // its Empty state renders centred — matches the /users, /accounts, /invoices, and
            // /billing-events layouts where Empty is a direct child of AppLayout's flex-1 body.
            <div className="flex min-h-0 flex-1 flex-col gap-8">
              {isPlanFeatureFlag ? (
                <PlanFeatureFlagInfoSection featureFlag={featureFlag} />
              ) : (
                <FeatureFlagInfoSection
                  featureFlag={featureFlag}
                  orphanedAt={featureFlag.deletedAt ?? featureFlag.orphanedAt}
                  canActivate={canActivate}
                />
              )}
              {isDeleted && (
                <div className="rounded-lg border border-muted-foreground/30 bg-muted/30 p-4 text-sm text-muted-foreground">
                  <Trans>
                    This flag has been deleted. It is retained for historical telemetry. Adding a new feature flag with
                    the same name will fail deployment.
                  </Trans>
                </div>
              )}
              {isOrphaned && (
                <div className="flex items-center justify-between gap-4 rounded-lg border border-destructive/30 bg-destructive/5 p-4">
                  <span className="text-sm text-destructive">
                    <Trans>
                      This flag no longer exists in code. Delete it to remove all account and user overrides.
                    </Trans>
                  </span>
                  <Button
                    variant="destructive"
                    className="shrink-0"
                    onClick={() => setIsDeleteDialogOpen(true)}
                    disabled={!canActivate}
                  >
                    <Trash2Icon className="size-4" aria-hidden={true} />
                    <Trans>Delete flag and all overrides</Trans>
                  </Button>
                </div>
              )}
              {!isDeleted && !isOrphaned && featureFlag.scope === "Tenant" && !isPlanFeatureFlag && (
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
                  orderBy={tenantsOrderBy}
                  sortOrder={tenantsSortOrder}
                />
              )}
              {!isDeleted && !isOrphaned && featureFlag.scope === "Tenant" && isPlanFeatureFlag && (
                <PlanFeatureFlagTenantsSection
                  flagKey={featureFlag.key}
                  requiredPlan={featureFlag.requiredPlan}
                  search={tenantsSearch}
                  plans={tenantsPlans ?? []}
                  pageOffset={tenantsPageOffset}
                />
              )}
              {!isDeleted && !isOrphaned && featureFlag.scope === "User" && (
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
                  orderBy={usersOrderBy}
                  sortOrder={usersSortOrder}
                />
              )}
            </div>
          ) : null}
        </AppLayout>
      </SidebarInset>
      {featureFlag && isOrphaned && (
        <DeleteFeatureFlagDialog
          flagKey={featureFlag.key}
          flagName={featureFlagName}
          isOpen={isDeleteDialogOpen}
          onOpenChange={setIsDeleteDialogOpen}
        />
      )}
    </SidebarProvider>
  );
}
