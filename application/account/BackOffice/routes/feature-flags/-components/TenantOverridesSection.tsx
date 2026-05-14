import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { Empty, EmptyDescription, EmptyHeader, EmptyMedia, EmptyTitle } from "@repo/ui/components/Empty";
import { Skeleton } from "@repo/ui/components/Skeleton";
import { TablePagination } from "@repo/ui/components/TablePagination";
import { keepPreviousData } from "@tanstack/react-query";
import { useNavigate } from "@tanstack/react-router";
import { Building2Icon } from "lucide-react";
import { useCallback } from "react";

import type { SubscriptionPlan } from "@/shared/lib/api/client";

import { api } from "@/shared/lib/api/client";

import type { StateFilter } from "./stateFilter";

import { FeatureFlagTenantsToolbar } from "./FeatureFlagTenantsToolbar";
import { DEFAULT_STATE_FILTER, toApiState } from "./stateFilter";
import { TenantOverrideTable } from "./TenantOverrideTable";

interface TenantOverridesSectionProps {
  flagKey: string;
  featureFlagDescription: string;
  showRolloutBucket: boolean;
  isFeatureFlagActive: boolean;
  search: string | undefined;
  plans: SubscriptionPlan[];
  state: StateFilter | undefined;
  hasOverride: boolean;
  pageOffset: number | undefined;
}

export function TenantOverridesSection({
  flagKey,
  featureFlagDescription,
  showRolloutBucket,
  isFeatureFlagActive,
  search,
  plans,
  state,
  hasOverride,
  pageOffset
}: Readonly<TenantOverridesSectionProps>) {
  const navigate = useNavigate();

  const { data, isLoading } = api.useQuery(
    "get",
    "/api/back-office/feature-flags/{flagKey}/tenants",
    {
      params: {
        path: { flagKey },
        query: {
          Search: search,
          Plans: plans.length === 0 ? undefined : plans,
          State: toApiState(state),
          HasOverride: hasOverride ? true : undefined,
          PageOffset: pageOffset
        }
      }
    },
    { placeholderData: keepPreviousData }
  );

  const tenants = data?.tenants ?? [];
  const totalPages = data?.totalPages ?? 0;
  const currentPage = (data?.currentPageOffset ?? 0) + 1;
  const effectiveState = state ?? DEFAULT_STATE_FILTER;
  const hasFilters = Boolean(search) || plans.length > 0 || effectiveState !== DEFAULT_STATE_FILTER || hasOverride;

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
    <div className="flex flex-col gap-4">
      <div>
        <h3>
          <Trans>Account status</Trans>
        </h3>
        <p className="text-sm text-muted-foreground">
          {showRolloutBucket ? (
            <Trans>
              Accounts are automatically included based on their rollout bucket. Use overrides to manually include or
              exclude specific accounts.
            </Trans>
          ) : (
            <Trans>Toggle the override switch to enable this feature for specific accounts.</Trans>
          )}
        </p>
      </div>
      <FeatureFlagTenantsToolbar
        flagKey={flagKey}
        search={search}
        plans={plans}
        state={state}
        hasOverride={hasOverride}
      />
      {isLoading && tenants.length === 0 ? (
        <TenantOverridesSkeleton />
      ) : tenants.length === 0 ? (
        <TenantOverridesEmpty hasFilters={hasFilters} />
      ) : (
        <>
          <TenantOverrideTable
            ariaLabel={t`Accounts`}
            tenants={tenants}
            flagKey={flagKey}
            featureFlagDescription={featureFlagDescription}
            showRolloutBucket={showRolloutBucket}
            isFeatureFlagActive={isFeatureFlagActive}
          />
          {totalPages > 1 && (
            <TablePagination
              currentPage={currentPage}
              totalPages={totalPages}
              onPageChange={handlePageChange}
              previousLabel={t`Previous`}
              nextLabel={t`Next`}
              trackingTitle="Feature flag tenants"
              className="w-full"
            />
          )}
        </>
      )}
    </div>
  );
}

function TenantOverridesSkeleton() {
  return (
    <div className="flex flex-col gap-2">
      <Skeleton className="h-10 w-full rounded-md" />
      <Skeleton className="h-14 w-full rounded-md" />
      <Skeleton className="h-14 w-full rounded-md" />
    </div>
  );
}

function TenantOverridesEmpty({ hasFilters }: Readonly<{ hasFilters: boolean }>) {
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
    </Empty>
  );
}
