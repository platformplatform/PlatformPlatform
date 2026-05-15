import { Trans } from "@lingui/react/macro";
import { Table, TableBody, TableHeader, TableRow } from "@repo/ui/components/Table";
import { useNavigate } from "@tanstack/react-router";

import { SortableFeatureFlagUserProperties, SortOrder } from "@/shared/lib/api/client";

import type { FeatureFlagUserInfo } from "./types";

import { SortableHead } from "./SortableHead";
import { UserOverrideRow } from "./UserOverrideRow";

export interface UserOverridesTableProps {
  ariaLabel: string;
  users: FeatureFlagUserInfo[];
  flagKey: string;
  featureFlagDescription: string;
  showRolloutBucket: boolean;
  isFeatureFlagActive: boolean;
  orderBy: SortableFeatureFlagUserProperties | undefined;
  sortOrder: SortOrder | undefined;
  defaultOrderBy: SortableFeatureFlagUserProperties;
}

export function UserOverridesTable({
  ariaLabel,
  users,
  flagKey,
  featureFlagDescription,
  showRolloutBucket,
  isFeatureFlagActive,
  orderBy,
  sortOrder,
  defaultOrderBy
}: Readonly<UserOverridesTableProps>) {
  const navigate = useNavigate();
  const effectiveOrderBy = orderBy ?? defaultOrderBy;
  const effectiveSortOrder = sortOrder ?? SortOrder.Ascending;

  const handleSort = (column: SortableFeatureFlagUserProperties) => {
    let nextOrderBy: SortableFeatureFlagUserProperties | undefined = column;
    let nextSortOrder: SortOrder | undefined;
    if (effectiveOrderBy === column) {
      if (effectiveSortOrder === SortOrder.Ascending) {
        nextSortOrder = SortOrder.Descending;
      } else {
        nextOrderBy = undefined;
        nextSortOrder = undefined;
      }
    } else {
      nextSortOrder = SortOrder.Ascending;
    }
    navigate({
      to: "/feature-flags/$flagKey",
      params: { flagKey },
      search: (previous) => ({
        ...previous,
        usersOrderBy: nextOrderBy,
        usersSortOrder: nextSortOrder,
        usersPageOffset: undefined
      })
    });
  };

  return (
    <Table rowSize="compact" aria-label={ariaLabel} className="w-full table-fixed" containerClassName="overflow-x-clip">
      <TableHeader>
        <TableRow>
          <SortableHead
            column={SortableFeatureFlagUserProperties.Name}
            effectiveOrderBy={effectiveOrderBy}
            effectiveSortOrder={effectiveSortOrder}
            onSort={handleSort}
          >
            <Trans>User</Trans>
          </SortableHead>
          <SortableHead
            column={SortableFeatureFlagUserProperties.TenantName}
            effectiveOrderBy={effectiveOrderBy}
            effectiveSortOrder={effectiveSortOrder}
            onSort={handleSort}
            className="hidden md:table-cell"
          >
            <Trans>Account</Trans>
          </SortableHead>
          <SortableHead
            column={SortableFeatureFlagUserProperties.Role}
            effectiveOrderBy={effectiveOrderBy}
            effectiveSortOrder={effectiveSortOrder}
            onSort={handleSort}
            className="hidden w-[6rem] lg:table-cell"
          >
            <Trans>Role</Trans>
          </SortableHead>
          <SortableHead
            column={SortableFeatureFlagUserProperties.OverrideUpdatedAt}
            effectiveOrderBy={effectiveOrderBy}
            effectiveSortOrder={effectiveSortOrder}
            onSort={handleSort}
            className="hidden w-[8.5rem] lg:table-cell"
          >
            <Trans>Last changed</Trans>
          </SortableHead>
          {showRolloutBucket && (
            <SortableHead
              column={SortableFeatureFlagUserProperties.InclusionThresholdPercentage}
              effectiveOrderBy={effectiveOrderBy}
              effectiveSortOrder={effectiveSortOrder}
              onSort={handleSort}
              className="hidden w-[7rem] text-center lg:table-cell"
            >
              <Trans>Included at</Trans>
            </SortableHead>
          )}
          <SortableHead
            column={SortableFeatureFlagUserProperties.IsEnabled}
            effectiveOrderBy={effectiveOrderBy}
            effectiveSortOrder={effectiveSortOrder}
            onSort={handleSort}
            className="w-[5rem] text-right"
          >
            <Trans>Override</Trans>
          </SortableHead>
        </TableRow>
      </TableHeader>
      <TableBody>
        {users.map((user) => (
          <UserOverrideRow
            key={user.id}
            flagKey={flagKey}
            featureFlagDescription={featureFlagDescription}
            user={user}
            showRolloutBucket={showRolloutBucket}
            isFeatureFlagActive={isFeatureFlagActive}
          />
        ))}
      </TableBody>
    </Table>
  );
}
