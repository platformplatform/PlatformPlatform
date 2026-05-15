import { Trans } from "@lingui/react/macro";
import { Table, TableBody, TableHead, TableHeader, TableRow } from "@repo/ui/components/Table";
import { useNavigate } from "@tanstack/react-router";

import { SortableFeatureFlagTenantProperties, SortOrder } from "@/shared/lib/api/client";

import type { FeatureFlagTenantInfo } from "./types";

import { SortableHead } from "./SortableHead";
import { TenantOverrideRow } from "./TenantOverrideRow";

export interface TenantOverrideTableProps {
  ariaLabel: string;
  tenants: FeatureFlagTenantInfo[];
  flagKey: string;
  featureFlagDescription: string;
  showRolloutBucket: boolean;
  isFeatureFlagActive: boolean;
  orderBy: SortableFeatureFlagTenantProperties | undefined;
  sortOrder: SortOrder | undefined;
  defaultOrderBy: SortableFeatureFlagTenantProperties;
}

export function TenantOverrideTable({
  ariaLabel,
  tenants,
  flagKey,
  featureFlagDescription,
  showRolloutBucket,
  isFeatureFlagActive,
  orderBy,
  sortOrder,
  defaultOrderBy
}: Readonly<TenantOverrideTableProps>) {
  const navigate = useNavigate();
  const effectiveOrderBy = orderBy ?? defaultOrderBy;
  const effectiveSortOrder = sortOrder ?? SortOrder.Ascending;

  // Click cycle: same column flips asc <-> desc, second-flip clears to default; different column resets to asc.
  const handleSort = (column: SortableFeatureFlagTenantProperties) => {
    let nextOrderBy: SortableFeatureFlagTenantProperties | undefined = column;
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
        tenantsOrderBy: nextOrderBy,
        tenantsSortOrder: nextSortOrder,
        tenantsPageOffset: undefined
      })
    });
  };

  return (
    <Table rowSize="compact" aria-label={ariaLabel} className="w-full table-fixed" containerClassName="overflow-x-clip">
      <TableHeader>
        <TableRow>
          <SortableHead
            column={SortableFeatureFlagTenantProperties.Name}
            effectiveOrderBy={effectiveOrderBy}
            effectiveSortOrder={effectiveSortOrder}
            onSort={handleSort}
          >
            <Trans>Account</Trans>
          </SortableHead>
          <SortableHead
            column={SortableFeatureFlagTenantProperties.Plan}
            effectiveOrderBy={effectiveOrderBy}
            effectiveSortOrder={effectiveSortOrder}
            onSort={handleSort}
            className="hidden w-[6rem] md:table-cell"
          >
            <Trans>Plan</Trans>
          </SortableHead>
          <TableHead className="hidden w-[6rem] md:table-cell">
            <Trans>Status</Trans>
          </TableHead>
          <SortableHead
            column={SortableFeatureFlagTenantProperties.OverrideUpdatedAt}
            effectiveOrderBy={effectiveOrderBy}
            effectiveSortOrder={effectiveSortOrder}
            onSort={handleSort}
            className="hidden w-[8rem] lg:table-cell"
          >
            <Trans>Last changed</Trans>
          </SortableHead>
          {showRolloutBucket && (
            <SortableHead
              column={SortableFeatureFlagTenantProperties.InclusionThresholdPercentage}
              effectiveOrderBy={effectiveOrderBy}
              effectiveSortOrder={effectiveSortOrder}
              onSort={handleSort}
              className="hidden w-[7rem] text-center lg:table-cell"
            >
              <Trans>Included at</Trans>
            </SortableHead>
          )}
          <SortableHead
            column={SortableFeatureFlagTenantProperties.IsEnabled}
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
        {tenants.map((tenant) => (
          <TenantOverrideRow
            key={tenant.id}
            flagKey={flagKey}
            featureFlagDescription={featureFlagDescription}
            tenant={tenant}
            showRolloutBucket={showRolloutBucket}
            isFeatureFlagActive={isFeatureFlagActive}
          />
        ))}
      </TableBody>
    </Table>
  );
}
