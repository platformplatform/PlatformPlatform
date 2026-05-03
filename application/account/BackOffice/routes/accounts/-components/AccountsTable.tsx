import type { RowKey } from "@repo/ui/components/Table";

import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { Skeleton } from "@repo/ui/components/Skeleton";
import { Table, TableBody, TableHead, TableHeader, TableRow } from "@repo/ui/components/Table";
import { TablePagination } from "@repo/ui/components/TablePagination";
import { useFormatDate } from "@repo/ui/hooks/useSmartDate";
import { useNavigate } from "@tanstack/react-router";
import { useCallback, useMemo } from "react";

import type { components } from "@/shared/lib/api/client";

import { SortableTenantProperties, SortOrder } from "@/shared/lib/api/client";

import { AccountsTableRow } from "./AccountsTableRow";
import { SortableTableHead } from "./SortableTableHead";

type TenantSummary = components["schemas"]["TenantSummary"];

interface AccountsTableProps {
  tenants: TenantSummary[];
  isLoading: boolean;
  totalPages: number;
  currentPageOffset: number;
  selectedTenantId: string | undefined;
  onSelectTenant: (tenant: TenantSummary | null) => void;
  orderBy: SortableTenantProperties | undefined;
  sortOrder: SortOrder | undefined;
}

export function AccountsTable({
  tenants,
  isLoading,
  totalPages,
  currentPageOffset,
  selectedTenantId,
  onSelectTenant,
  orderBy,
  sortOrder
}: Readonly<AccountsTableProps>) {
  const navigate = useNavigate();
  const formatDate = useFormatDate();

  const selectedKeys = useMemo<ReadonlySet<RowKey>>(
    () => (selectedTenantId ? new Set<RowKey>([selectedTenantId]) : new Set<RowKey>()),
    [selectedTenantId]
  );

  const handleSelectionChange = useCallback(
    (keys: Set<RowKey>) => {
      if (keys.size === 0) {
        onSelectTenant(null);
        return;
      }
      const [first] = keys;
      const tenant = tenants.find((entry) => entry.id === first);
      onSelectTenant(tenant ?? null);
    },
    [onSelectTenant, tenants]
  );

  const handleActivate = useCallback(
    (key: RowKey) => {
      const tenant = tenants.find((entry) => entry.id === key);
      onSelectTenant(selectedTenantId === key ? null : (tenant ?? null));
    },
    [onSelectTenant, selectedTenantId, tenants]
  );

  const handlePageChange = useCallback(
    (page: number) => {
      navigate({
        to: "/accounts",
        search: (previous) => ({
          ...previous,
          pageOffset: page === 1 ? undefined : page - 1
        })
      });
    },
    [navigate]
  );

  const handleSort = useCallback(
    (column: SortableTenantProperties) => {
      const isCurrent = orderBy === column;
      const nextOrder = isCurrent && sortOrder === SortOrder.Descending ? SortOrder.Ascending : SortOrder.Descending;
      navigate({
        to: "/accounts",
        search: (previous) => ({
          ...previous,
          orderBy: column,
          sortOrder: nextOrder === SortOrder.Ascending ? undefined : nextOrder,
          pageOffset: undefined
        })
      });
    },
    [navigate, orderBy, sortOrder]
  );

  if (isLoading && tenants.length === 0) {
    return (
      <div className="flex flex-1 flex-col gap-2 p-2">
        {Array.from({ length: 8 }).map((_, index) => (
          <Skeleton key={`skeleton-${index}`} className="h-12 w-full" />
        ))}
      </div>
    );
  }

  const currentPage = currentPageOffset + 1;

  return (
    <>
      <div className="flex-1 overflow-visible rounded-md bg-background sm:min-h-48 sm:overflow-auto">
        <Table
          rowSize="spacious"
          aria-label={t`Accounts`}
          selectionMode="single"
          selectedKeys={selectedKeys}
          onSelectionChange={handleSelectionChange}
          onActivate={handleActivate}
          activateOnNavigate={selectedTenantId != null}
          scrollToKey={selectedTenantId}
          stickyHeader={true}
        >
          <TableHeader className="z-10 bg-inherit sm:sticky sm:top-0">
            <TableRow>
              <SortableTableHead
                column={SortableTenantProperties.Name}
                orderBy={orderBy}
                sortOrder={sortOrder}
                onSort={handleSort}
              >
                <Trans>Name</Trans>
              </SortableTableHead>
              <TableHead className="hidden md:table-cell">
                <Trans>Plan</Trans>
              </TableHead>
              <SortableTableHead
                column={SortableTenantProperties.MonthlyRecurringRevenue}
                orderBy={orderBy}
                sortOrder={sortOrder}
                onSort={handleSort}
                className="hidden md:table-cell"
              >
                <Trans>MRR</Trans>
              </SortableTableHead>
              <TableHead className="hidden lg:table-cell">
                <Trans>Renewal</Trans>
              </TableHead>
              <TableHead className="hidden lg:table-cell">
                <Trans>Country</Trans>
              </TableHead>
              <SortableTableHead
                column={SortableTenantProperties.CreatedAt}
                orderBy={orderBy}
                sortOrder={sortOrder}
                onSort={handleSort}
                className="hidden xl:table-cell"
              >
                <Trans>Created</Trans>
              </SortableTableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {tenants.map((tenant) => (
              <AccountsTableRow key={tenant.id} tenant={tenant} formatDate={formatDate} />
            ))}
          </TableBody>
        </Table>
      </div>

      {totalPages > 1 && (
        <div className="shrink-0 pt-4">
          <TablePagination
            currentPage={currentPage}
            totalPages={totalPages}
            onPageChange={handlePageChange}
            previousLabel={t`Previous`}
            nextLabel={t`Next`}
            trackingTitle="Accounts"
            className="w-full"
          />
        </div>
      )}
    </>
  );
}
