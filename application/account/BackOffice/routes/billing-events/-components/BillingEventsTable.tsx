import { t } from "@lingui/core/macro";
import { Skeleton } from "@repo/ui/components/Skeleton";
import { Table, TableBody } from "@repo/ui/components/Table";
import { TablePagination } from "@repo/ui/components/TablePagination";
import { useFormatDate } from "@repo/ui/hooks/useSmartDate";
import { useNavigate } from "@tanstack/react-router";
import { useCallback } from "react";

import type { components, SortableBillingEventProperties } from "@/shared/lib/api/client";

import { SortOrder } from "@/shared/lib/api/client";

import type { BillingEventsView } from "./BillingEventsToolbar";

import { BillingEventsTableColumnHeaders } from "./BillingEventsTableColumnHeaders";
import { BillingEventsTableRow } from "./BillingEventsTableRow";

type BillingEventSummary = components["schemas"]["BillingEventSummary"];

interface BillingEventsTableProps {
  billingEvents: BillingEventSummary[];
  isLoading: boolean;
  totalPages: number;
  currentPageOffset: number;
  orderBy: SortableBillingEventProperties | undefined;
  sortOrder: SortOrder | undefined;
}

export function BillingEventsTable({
  billingEvents,
  isLoading,
  totalPages,
  currentPageOffset,
  orderBy,
  sortOrder
}: Readonly<BillingEventsTableProps>) {
  const navigate = useNavigate();
  const formatDate = useFormatDate();

  const handlePageChange = useCallback(
    (page: number) => {
      navigate({
        to: "/billing-events",
        search: (previous) => ({
          search: previous.search,
          view: previous.view as BillingEventsView | undefined,
          orderBy: previous.orderBy as SortableBillingEventProperties | undefined,
          sortOrder: previous.sortOrder,
          pageOffset: page === 1 ? undefined : page - 1
        })
      });
    },
    [navigate]
  );

  const handleSort = useCallback(
    (column: SortableBillingEventProperties) => {
      // Backend default is Descending, so the URL stores Descending as undefined.
      // Treat both undefined and explicit Descending as the descending state when computing the next direction.
      const isCurrent = orderBy === column;
      const isCurrentlyDescending = (sortOrder ?? SortOrder.Descending) === SortOrder.Descending;
      const nextOrder = isCurrent && isCurrentlyDescending ? SortOrder.Ascending : SortOrder.Descending;
      navigate({
        to: "/billing-events",
        search: (previous) => ({
          search: previous.search,
          view: previous.view as BillingEventsView | undefined,
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
      navigate({ to: "/accounts/$tenantId", params: { tenantId }, search: { tab: "billing-events" } });
    },
    [navigate]
  );

  if (isLoading && billingEvents.length === 0) {
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
          className="min-w-[32rem] table-fixed md:min-w-[48rem] xl:min-w-[53rem]"
          rowSize="spacious"
          aria-label={t`Billing events`}
          stickyHeader={true}
        >
          <BillingEventsTableColumnHeaders orderBy={orderBy} sortOrder={sortOrder} onSort={handleSort} />
          <TableBody>
            {billingEvents.map((event) => (
              <BillingEventsTableRow key={event.id} event={event} formatDate={formatDate} onRowClick={handleRowClick} />
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
            trackingTitle="Billing events"
            className="w-full"
          />
        </div>
      )}
    </>
  );
}
