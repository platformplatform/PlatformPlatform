import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { Skeleton } from "@repo/ui/components/Skeleton";
import { TablePagination } from "@repo/ui/components/TablePagination";
import { keepPreviousData } from "@tanstack/react-query";
import { useNavigate } from "@tanstack/react-router";
import { useCallback } from "react";

import type { SortOrder, UserRole } from "@/shared/lib/api/client";

import { api, SortableFeatureFlagUserProperties } from "@/shared/lib/api/client";

import type { StateFilter } from "./stateFilter";

import { FeatureFlagUsersEmpty } from "./FeatureFlagUsersEmpty";
import { FeatureFlagUsersToolbar } from "./FeatureFlagUsersToolbar";
import { DEFAULT_STATE_FILTER, toApiState } from "./stateFilter";
import { UserOverridesTable } from "./UserOverridesTable";

interface UserOverridesSectionProps {
  flagKey: string;
  featureFlagDescription: string;
  showRolloutBucket: boolean;
  isFeatureFlagActive: boolean;
  search: string | undefined;
  roles: UserRole[];
  state: StateFilter | undefined;
  hasOverride: boolean;
  pageOffset: number | undefined;
  orderBy: SortableFeatureFlagUserProperties | undefined;
  sortOrder: SortOrder | undefined;
}

export function UserOverridesSection({
  flagKey,
  featureFlagDescription,
  showRolloutBucket,
  isFeatureFlagActive,
  search,
  roles,
  state,
  hasOverride,
  pageOffset,
  orderBy,
  sortOrder
}: Readonly<UserOverridesSectionProps>) {
  const navigate = useNavigate();

  const defaultOrderBy = showRolloutBucket
    ? SortableFeatureFlagUserProperties.InclusionThresholdPercentage
    : SortableFeatureFlagUserProperties.Name;
  const effectiveOrderBy = orderBy ?? defaultOrderBy;

  const { data, isLoading } = api.useQuery(
    "get",
    "/api/back-office/feature-flags/{flagKey}/users",
    {
      params: {
        path: { flagKey },
        query: {
          Search: search,
          Roles: roles.length === 0 ? undefined : roles,
          State: toApiState(state),
          HasOverride: hasOverride ? true : undefined,
          PageOffset: pageOffset,
          OrderBy: effectiveOrderBy,
          SortOrder: sortOrder
        }
      }
    },
    { placeholderData: keepPreviousData }
  );

  const users = data?.users ?? [];
  const totalPages = data?.totalPages ?? 0;
  const currentPage = (data?.currentPageOffset ?? 0) + 1;
  const effectiveState = state ?? DEFAULT_STATE_FILTER;
  const hasFilters = Boolean(search) || roles.length > 0 || effectiveState !== DEFAULT_STATE_FILTER || hasOverride;

  const handlePageChange = useCallback(
    (page: number) => {
      navigate({
        to: "/feature-flags/$flagKey",
        params: { flagKey },
        search: (previous) => ({
          ...previous,
          usersPageOffset: page === 1 ? undefined : page - 1
        })
      });
    },
    [navigate, flagKey]
  );

  return (
    <div className="flex min-h-0 flex-1 flex-col gap-4">
      <div>
        <h3>
          <Trans>User status</Trans>
        </h3>
        <p className="text-sm text-muted-foreground">
          {showRolloutBucket ? (
            <Trans>
              Users are automatically included based on their rollout bucket. Use overrides to manually include or
              exclude specific users.
            </Trans>
          ) : (
            <Trans>Toggle the override switch to enable this feature for specific users.</Trans>
          )}
        </p>
      </div>
      <FeatureFlagUsersToolbar
        flagKey={flagKey}
        search={search}
        roles={roles}
        state={state}
        hasOverride={hasOverride}
        hideHasOverride={!showRolloutBucket}
      />
      {isLoading && users.length === 0 ? (
        <UserOverridesSkeleton />
      ) : users.length === 0 ? (
        <FeatureFlagUsersEmpty flagKey={flagKey} hasFilters={hasFilters} />
      ) : (
        <>
          <div className="flex-1 overflow-visible sm:min-h-48 sm:overflow-auto">
            <UserOverridesTable
              ariaLabel={t`Users`}
              users={users}
              flagKey={flagKey}
              featureFlagDescription={featureFlagDescription}
              showRolloutBucket={showRolloutBucket}
              isFeatureFlagActive={isFeatureFlagActive}
              orderBy={orderBy}
              sortOrder={sortOrder}
              defaultOrderBy={defaultOrderBy}
            />
          </div>
          {totalPages > 1 && (
            <div className="shrink-0 pt-4">
              <TablePagination
                currentPage={currentPage}
                totalPages={totalPages}
                onPageChange={handlePageChange}
                previousLabel={t`Previous`}
                nextLabel={t`Next`}
                trackingTitle="Feature flag users"
                className="w-full"
              />
            </div>
          )}
        </>
      )}
    </div>
  );
}

function UserOverridesSkeleton() {
  return (
    <div className="flex flex-col gap-2">
      <Skeleton className="h-10 w-full rounded-md" />
      <Skeleton className="h-14 w-full rounded-md" />
      <Skeleton className="h-14 w-full rounded-md" />
    </div>
  );
}
