import type { RowKey } from "@repo/ui/components/Table";

import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { Skeleton } from "@repo/ui/components/Skeleton";
import { Table, TableBody, TableHeader, TableRow } from "@repo/ui/components/Table";
import { TablePagination } from "@repo/ui/components/TablePagination";
import { useCallback } from "react";

import type { Schemas, SortOrder } from "@/shared/lib/api/client";

import { SortableTicketProperties } from "@/shared/lib/api/client";

import { InboxTableRow } from "./InboxTableRow";
import { SortableInboxTableHead } from "./SortableInboxTableHead";

type Ticket = Schemas["AllTicketsSummary"];

interface InboxTableProps {
  tickets: Ticket[];
  isLoading: boolean;
  totalPages: number;
  currentPageOffset: number;
  selectedTicketId: string | undefined;
  orderBy: SortableTicketProperties;
  sortOrder: SortOrder;
  onSelectTicket: (ticketId: string | undefined) => void;
  onPageChange: (page: number) => void;
  onSort: (column: SortableTicketProperties) => void;
}

export function InboxTable({
  tickets,
  isLoading,
  totalPages,
  currentPageOffset,
  selectedTicketId,
  orderBy,
  sortOrder,
  onSelectTicket,
  onPageChange,
  onSort
}: Readonly<InboxTableProps>) {
  const handleSelectionChange = useCallback(
    (keys: Set<RowKey>) => {
      const next = keys.size === 0 ? undefined : (Array.from(keys)[0] as string);
      onSelectTicket(next);
    },
    [onSelectTicket]
  );

  if (isLoading && tickets.length === 0) {
    return (
      <div className="flex flex-1 flex-col gap-2 p-2">
        {Array.from({ length: 8 }).map((_, index) => (
          <Skeleton key={`skeleton-${index}`} className="h-12 w-full" />
        ))}
      </div>
    );
  }

  const currentPage = currentPageOffset + 1;
  const selectedKeys = selectedTicketId ? new Set<RowKey>([selectedTicketId]) : new Set<RowKey>();

  return (
    <>
      <div className="flex-1 overflow-visible sm:min-h-48 sm:overflow-auto">
        <Table
          rowSize="spacious"
          aria-label={t`Support tickets`}
          selectionMode="single"
          selectedKeys={selectedKeys}
          onSelectionChange={handleSelectionChange}
          stickyHeader={true}
        >
          <TableHeader>
            <TableRow>
              <SortableInboxTableHead
                column={SortableTicketProperties.Assignee}
                orderBy={orderBy}
                sortOrder={sortOrder}
                onSort={onSort}
                className="w-12"
              >
                <span className="sr-only">
                  <Trans>Assignee</Trans>
                </span>
              </SortableInboxTableHead>
              <SortableInboxTableHead
                column={SortableTicketProperties.Subject}
                orderBy={orderBy}
                sortOrder={sortOrder}
                onSort={onSort}
              >
                <Trans>Subject</Trans>
              </SortableInboxTableHead>
              <SortableInboxTableHead
                column={SortableTicketProperties.Reporter}
                orderBy={orderBy}
                sortOrder={sortOrder}
                onSort={onSort}
                className="hidden md:table-cell"
              >
                <Trans>Reporter</Trans>
              </SortableInboxTableHead>
              <SortableInboxTableHead
                column={SortableTicketProperties.Tenant}
                orderBy={orderBy}
                sortOrder={sortOrder}
                onSort={onSort}
                className="hidden lg:table-cell"
              >
                <Trans>Account</Trans>
              </SortableInboxTableHead>
              <SortableInboxTableHead
                column={SortableTicketProperties.Status}
                orderBy={orderBy}
                sortOrder={sortOrder}
                onSort={onSort}
              >
                <Trans>Status</Trans>
              </SortableInboxTableHead>
              <SortableInboxTableHead
                column={SortableTicketProperties.Created}
                orderBy={orderBy}
                sortOrder={sortOrder}
                onSort={onSort}
                className="hidden xl:table-cell"
              >
                <Trans>Opened</Trans>
              </SortableInboxTableHead>
              <SortableInboxTableHead
                column={SortableTicketProperties.LastActivity}
                orderBy={orderBy}
                sortOrder={sortOrder}
                onSort={onSort}
              >
                <Trans>Last activity</Trans>
              </SortableInboxTableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {tickets.map((ticket) => (
              <InboxTableRow key={ticket.id} ticket={ticket} />
            ))}
          </TableBody>
        </Table>
      </div>

      {totalPages > 1 && (
        <div className="shrink-0 pt-4">
          <TablePagination
            currentPage={currentPage}
            totalPages={totalPages}
            onPageChange={onPageChange}
            previousLabel={t`Previous`}
            nextLabel={t`Next`}
            trackingTitle="Support tickets"
            className="w-full"
          />
        </div>
      )}
    </>
  );
}
