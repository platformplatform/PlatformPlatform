import { t } from "@lingui/core/macro";
import { Skeleton } from "@repo/ui/components/Skeleton";
import { Table, TableBody } from "@repo/ui/components/Table";
import { TablePagination } from "@repo/ui/components/TablePagination";
import { useNavigate } from "@tanstack/react-router";
import { useCallback } from "react";

import type { components, SortableBackOfficeInvoiceProperties } from "@/shared/lib/api/client";

import { SortOrder } from "@/shared/lib/api/client";

import type { InvoicesView } from "./InvoicesToolbar";

import { InvoicesTableColumnHeaders } from "./InvoicesTableColumnHeaders";
import { InvoicesTableRow } from "./InvoicesTableRow";

type Invoice = components["schemas"]["BackOfficeInvoiceSummary"];

interface InvoicesTableProps {
  invoices: Invoice[];
  isLoading: boolean;
  totalPages: number;
  currentPageOffset: number;
  orderBy: SortableBackOfficeInvoiceProperties | undefined;
  sortOrder: SortOrder | undefined;
}

export function InvoicesTable({
  invoices,
  isLoading,
  totalPages,
  currentPageOffset,
  orderBy,
  sortOrder
}: Readonly<InvoicesTableProps>) {
  const navigate = useNavigate();

  const handlePageChange = useCallback(
    (page: number) => {
      navigate({
        to: "/invoices",
        search: (previous) => ({
          search: previous.search,
          view: previous.view as InvoicesView | undefined,
          orderBy: previous.orderBy as SortableBackOfficeInvoiceProperties | undefined,
          sortOrder: previous.sortOrder,
          pageOffset: page === 1 ? undefined : page - 1
        })
      });
    },
    [navigate]
  );

  const handleSort = useCallback(
    (column: SortableBackOfficeInvoiceProperties) => {
      // Backend default is Descending, so the URL stores Descending as undefined; treat both undefined and
      // explicit Descending as the descending state when computing the next direction.
      const isCurrent = orderBy === column;
      const isCurrentlyDescending = (sortOrder ?? SortOrder.Descending) === SortOrder.Descending;
      const nextOrder = isCurrent && isCurrentlyDescending ? SortOrder.Ascending : SortOrder.Descending;
      navigate({
        to: "/invoices",
        search: (previous) => ({
          search: previous.search,
          view: previous.view as InvoicesView | undefined,
          orderBy: column,
          sortOrder: nextOrder === SortOrder.Descending ? undefined : nextOrder,
          pageOffset: undefined
        })
      });
    },
    [navigate, orderBy, sortOrder]
  );

  const handleRowClick = useCallback(
    (tenantId: string) => {
      navigate({ to: "/accounts/$tenantId", params: { tenantId }, search: { tab: "invoices" } });
    },
    [navigate]
  );

  if (isLoading && invoices.length === 0) {
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
          className="min-w-[32rem] table-fixed md:min-w-[48rem] xl:min-w-[58rem]"
          rowSize="spacious"
          aria-label={t`Invoices`}
          stickyHeader={true}
        >
          <InvoicesTableColumnHeaders orderBy={orderBy} sortOrder={sortOrder} onSort={handleSort} />
          <TableBody>
            {invoices.map((invoice) => (
              <InvoicesTableRow
                key={`${invoice.id}-${invoice.rowKind}`}
                invoice={invoice}
                onRowClick={handleRowClick}
              />
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
            trackingTitle="Invoices"
            className="w-full"
          />
        </div>
      )}
    </>
  );
}
