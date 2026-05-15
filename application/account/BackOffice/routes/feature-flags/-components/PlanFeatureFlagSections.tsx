import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { Badge } from "@repo/ui/components/Badge";
import { Button } from "@repo/ui/components/Button";
import { Empty, EmptyContent, EmptyDescription, EmptyHeader, EmptyMedia, EmptyTitle } from "@repo/ui/components/Empty";
import { Skeleton } from "@repo/ui/components/Skeleton";
import { TablePagination } from "@repo/ui/components/TablePagination";
import { keepPreviousData } from "@tanstack/react-query";
import { useNavigate } from "@tanstack/react-router";
import { Building2Icon } from "lucide-react";
import { useCallback } from "react";

import { api, SubscriptionPlan } from "@/shared/lib/api/client";
import { getSubscriptionPlanLabel } from "@/shared/lib/api/labels";
import { getSubscriptionPlanBadgeClass } from "@/shared/lib/planBadge";

import type { FeatureFlagInfo } from "./types";

import { FeatureFlagTenantsToolbar } from "./FeatureFlagTenantsToolbar";
import { PlanFeatureFlagTenantTable } from "./PlanFeatureFlagTenantTable";

// Ordered low → high tier. A plan-gated feature is enabled for accounts on the required plan or any
// higher plan, so the default selection on entry is everything from the required plan upward.
const PLAN_ORDER = [SubscriptionPlan.Basis, SubscriptionPlan.Standard, SubscriptionPlan.Premium] as const;

function plansAtOrAboveRequired(requiredPlan: string | null): SubscriptionPlan[] {
  if (!requiredPlan) return [];
  const idx = PLAN_ORDER.indexOf(requiredPlan as SubscriptionPlan);
  if (idx < 0) return [];
  return PLAN_ORDER.slice(idx);
}

export function PlanFeatureFlagInfoSection({ featureFlag }: Readonly<{ featureFlag: FeatureFlagInfo }>) {
  return (
    <div className="flex flex-col gap-4">
      <div className="flex flex-col gap-4 lg:flex-row lg:items-center lg:justify-between">
        <div className="flex flex-col gap-0.5 text-sm text-muted-foreground">
          <span>
            <Trans>Name:</Trans> <span className="font-mono">{featureFlag.key}</span>
          </span>
          <span>
            <Trans>Required plan:</Trans>{" "}
            {featureFlag.requiredPlan !== null && (
              <Badge className={getSubscriptionPlanBadgeClass(featureFlag.requiredPlan as SubscriptionPlan)}>
                {getSubscriptionPlanLabel(featureFlag.requiredPlan as SubscriptionPlan)}
              </Badge>
            )}
          </span>
        </div>
        <Badge variant={featureFlag.isActive ? "default" : "outline"}>
          {featureFlag.isActive ? t`Active` : t`Inactive`}
        </Badge>
      </div>
      <p className="text-sm text-muted-foreground">
        <Trans>
          This flag is managed by the subscription plan. It is automatically enabled for accounts on the required plan
          or higher.
        </Trans>
      </p>
    </div>
  );
}

interface PlanFeatureFlagTenantsSectionProps {
  flagKey: string;
  requiredPlan: string | null;
  search: string | undefined;
  plans: SubscriptionPlan[];
  pageOffset: number | undefined;
}

export function PlanFeatureFlagTenantsSection({
  flagKey,
  requiredPlan,
  search,
  plans,
  pageOffset
}: Readonly<PlanFeatureFlagTenantsSectionProps>) {
  const navigate = useNavigate();

  // On entry (no plans param in URL) pre-select the plans the flag is active for — for plan-gated
  // flags those are the only accounts where the feature is relevant. If the user explicitly clears
  // every chip the toolbar drops the URL param, which we treat as "back to the default selection";
  // they always have to pass through "everything selected" or another chip combination to see other
  // accounts.
  const effectivePlans = plans.length === 0 ? plansAtOrAboveRequired(requiredPlan) : plans;

  const { data, isLoading } = api.useQuery(
    "get",
    "/api/back-office/feature-flags/{flagKey}/tenants",
    {
      params: {
        path: { flagKey },
        query: {
          Search: search,
          Plans: effectivePlans.length === 0 ? undefined : effectivePlans,
          PageOffset: pageOffset
        }
      }
    },
    { placeholderData: keepPreviousData }
  );

  const tenants = data?.tenants ?? [];
  const totalPages = data?.totalPages ?? 0;
  const currentPage = (data?.currentPageOffset ?? 0) + 1;
  const hasFilters = Boolean(search) || plans.length > 0;

  const handlePageChange = useCallback(
    (page: number) => {
      navigate({
        to: "/feature-flags/$flagKey",
        params: { flagKey },
        search: (previous) => ({
          ...previous,
          tenantsPageOffset: page === 1 ? undefined : page - 1
        })
      });
    },
    [navigate, flagKey]
  );

  return (
    <div className="flex min-h-0 flex-1 flex-col gap-4">
      <div>
        <h3>
          <Trans>Account status</Trans>
        </h3>
        <p className="text-sm text-muted-foreground">
          <Trans>
            Accounts are automatically enabled or disabled based on their subscription plan. No manual overrides are
            available for plan-managed flags.
          </Trans>
        </p>
      </div>
      <FeatureFlagTenantsToolbar
        flagKey={flagKey}
        search={search}
        plans={effectivePlans}
        state={undefined}
        hasOverride={false}
        hideHasOverride={true}
        hideState={true}
      />
      {isLoading && tenants.length === 0 ? (
        <PlanFeatureFlagTenantsSkeleton />
      ) : tenants.length === 0 ? (
        <PlanFeatureFlagTenantsEmpty flagKey={flagKey} hasFilters={hasFilters} />
      ) : (
        <>
          <PlanFeatureFlagTenantTable ariaLabel={t`Accounts`} tenants={tenants} />
          {totalPages > 1 && (
            <TablePagination
              currentPage={currentPage}
              totalPages={totalPages}
              onPageChange={handlePageChange}
              previousLabel={t`Previous`}
              nextLabel={t`Next`}
              trackingTitle="Plan feature flag tenants"
              className="w-full"
            />
          )}
        </>
      )}
    </div>
  );
}

function PlanFeatureFlagTenantsSkeleton() {
  return (
    <div className="flex flex-col gap-2">
      <Skeleton className="h-10 w-full rounded-md" />
      <Skeleton className="h-14 w-full rounded-md" />
      <Skeleton className="h-14 w-full rounded-md" />
    </div>
  );
}

function PlanFeatureFlagTenantsEmpty({ flagKey, hasFilters }: Readonly<{ flagKey: string; hasFilters: boolean }>) {
  const navigate = useNavigate();
  return (
    <Empty>
      <EmptyHeader>
        <EmptyMedia variant="icon">
          <Building2Icon />
        </EmptyMedia>
        <EmptyTitle>
          {hasFilters ? (
            <Trans>No accounts match your filters</Trans>
          ) : (
            <Trans>No accounts qualify for this feature yet</Trans>
          )}
        </EmptyTitle>
        <EmptyDescription>
          {hasFilters ? (
            <Trans>Try clearing the search or filters to see more results.</Trans>
          ) : (
            <Trans>Accounts will appear here as they qualify for this feature.</Trans>
          )}
        </EmptyDescription>
      </EmptyHeader>
      {hasFilters && (
        <EmptyContent>
          <Button
            variant="outline"
            size="sm"
            onClick={() =>
              navigate({
                to: "/feature-flags/$flagKey",
                params: { flagKey },
                search: () => ({})
              })
            }
          >
            <Trans>Clear filters</Trans>
          </Button>
        </EmptyContent>
      )}
    </Empty>
  );
}
