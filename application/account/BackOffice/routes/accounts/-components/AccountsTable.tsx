import type { RowKey } from "@repo/ui/components/Table";

import { t } from "@lingui/core/macro";
import { Skeleton } from "@repo/ui/components/Skeleton";
import { Table, TableBody } from "@repo/ui/components/Table";
import { TablePagination } from "@repo/ui/components/TablePagination";
import { useFormatDate } from "@repo/ui/hooks/useSmartDate";
import { useNavigate } from "@tanstack/react-router";
import { useCallback, useMemo } from "react";

import type { components, SortableTenantProperties } from "@/shared/lib/api/client";

import { SortOrder } from "@/shared/lib/api/client";

import { AccountsTableColumnHeaders } from "./AccountsTableColumnHeaders";
import { AccountsTableRow } from "./AccountsTableRow";

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
      onSelectTenant(tenant ?? null);
    },
    [onSelectTenant, tenants]
  );

  const handlePageChange = useCallback(
    (page: number) => {
      navigate({
        to: "/accounts",
        search: (previous) => ({
          ...previous,
          orderBy: previous.orderBy as SortableTenantProperties | undefined,
          pageOffset: page === 1 ? undefined : page - 1
        })
      });
    },
    [navigate]
  );

  const handleSort = useCallback(
    (column: SortableTenantProperties) => {
      // Backend default is Descending and the URL stores Descending as undefined; treat both undefined
      // and explicit Descending as the descending state when computing the next direction.
      const isCurrent = orderBy === column;
      const isCurrentlyDescending = (sortOrder ?? SortOrder.Descending) === SortOrder.Descending;
      const nextOrder = isCurrent && isCurrentlyDescending ? SortOrder.Ascending : SortOrder.Descending;
      navigate({
        to: "/accounts",
        search: (previous) => ({
          ...previous,
          orderBy: column,
          sortOrder: nextOrder === SortOrder.Descending ? undefined : nextOrder,
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
      <div className="flex-1 overflow-visible sm:min-h-48 sm:overflow-auto">
        <Table
          className="table-fixed"
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
          <AccountsTableColumnHeaders orderBy={orderBy} sortOrder={sortOrder} onSort={handleSort} />
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
