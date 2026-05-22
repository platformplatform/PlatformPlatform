import { t } from "@lingui/core/macro";
import { requireSupportSystemEnabled } from "@repo/infrastructure/auth/routeGuards";
import { AppLayout } from "@repo/ui/components/AppLayout";
import { SidebarInset, SidebarProvider } from "@repo/ui/components/Sidebar";
import { keepPreviousData } from "@tanstack/react-query";
import { createFileRoute, useNavigate, useRouterState } from "@tanstack/react-router";
import { z } from "zod";

import { BackOfficeSideMenu } from "@/shared/components/BackOfficeSideMenu";
import {
  api,
  SortableTicketProperties,
  SortOrder,
  SupportTicketAssigneeFilter,
  SupportTicketCategory,
  SupportTicketStatus
} from "@/shared/lib/api/client";

import { BackOfficeSupportSidePane } from "../-components/BackOfficeSupportSidePane";
import { InboxZeroEmpty, NoMatchesEmpty } from "../-components/InboxEmptyStates";
import { InboxStatTiles } from "../-components/InboxStatTiles";
import { InboxTable } from "../-components/InboxTable";
import { InboxToolbar } from "../-components/InboxToolbar";

const inboxSearchSchema = z.object({
  search: z.string().optional(),
  status: z.nativeEnum(SupportTicketStatus).optional(),
  category: z.nativeEnum(SupportTicketCategory).optional(),
  assignee: z.nativeEnum(SupportTicketAssigneeFilter).optional(),
  orderBy: z.nativeEnum(SortableTicketProperties).optional(),
  sortOrder: z.nativeEnum(SortOrder).optional(),
  pageOffset: z.number().int().nonnegative().optional(),
  selectedTicketId: z.string().optional()
});

const DEFAULT_ORDER_BY = SortableTicketProperties.LastActivity;
const DEFAULT_SORT_ORDER = SortOrder.Descending;

export const Route = createFileRoute("/support/tickets/")({
  staticData: { trackingTitle: "Support tickets" },
  validateSearch: inboxSearchSchema,
  beforeLoad: () => requireSupportSystemEnabled(),
  component: SupportInboxPage
});

function SupportInboxPage() {
  const { search, status, category, assignee, orderBy, sortOrder, pageOffset, selectedTicketId } = Route.useSearch();
  const navigate = useNavigate();
  const currentPathname = useRouterState({ select: (state) => state.location.pathname });
  const trimmed = search?.trim() ?? "";
  // Default values are stored in the URL as undefined to keep the canonical /support/tickets link
  // short, so derive an effective order/direction for the header chevron and the click-flip logic.
  const effectiveOrderBy = orderBy ?? DEFAULT_ORDER_BY;
  const effectiveSortOrder = sortOrder ?? DEFAULT_SORT_ORDER;

  const { data, isLoading } = api.useQuery(
    "get",
    "/api/back-office/support-tickets",
    {
      params: {
        query: {
          Search: trimmed.length > 0 ? trimmed : undefined,
          Status: status,
          Category: category,
          Assignee: assignee,
          OrderBy: orderBy,
          SortOrder: sortOrder,
          PageOffset: pageOffset
        }
      }
    },
    { placeholderData: keepPreviousData }
  );

  const tickets = data?.tickets ?? [];
  const hasActiveFilters =
    trimmed.length > 0 || status !== undefined || category !== undefined || assignee !== undefined;
  const showEmpty = !isLoading && tickets.length === 0 && !hasActiveFilters;
  const showNoResults = !isLoading && tickets.length === 0 && hasActiveFilters;

  // Every navigation except a row selection clears selectedTicketId: a status/sort/filter/page change
  // can drop the previewed ticket's row from the visible result set, leaving the side pane stranded on
  // a ticket the user can no longer see in the table.
  const handleStatusChange = (next: SupportTicketStatus | undefined) => {
    navigate({
      to: "/support/tickets",
      search: (previous) => ({
        ...previous,
        orderBy: previous.orderBy as SortableTicketProperties | undefined,
        status: next,
        pageOffset: undefined,
        selectedTicketId: undefined
      })
    });
  };

  const handleClearFilters = () => {
    navigate({ to: "/support/tickets", search: () => ({}) });
  };

  const handlePageChange = (page: number) => {
    navigate({
      to: "/support/tickets",
      search: (previous) => ({
        ...previous,
        orderBy: previous.orderBy as SortableTicketProperties | undefined,
        pageOffset: page === 1 ? undefined : page - 1,
        selectedTicketId: undefined
      })
    });
  };

  const handleSelectTicket = (ticketId: string | undefined) => {
    // SidePane fires onOpenChange(false) when navigating away (e.g. Open ticket deep-link); ignore
    // it once we've left the inbox, otherwise we'd re-navigate back and cancel the deep-link.
    if (currentPathname !== "/support/tickets") {
      return;
    }
    navigate({
      to: "/support/tickets",
      search: (previous) => ({
        ...previous,
        orderBy: previous.orderBy as SortableTicketProperties | undefined,
        selectedTicketId: ticketId
      })
    });
  };

  const handleSort = (column: SortableTicketProperties) => {
    // Flip direction when clicking the active column, otherwise default to the column's preferred
    // initial order. Defaults are stored as undefined in the URL so /support/tickets stays canonical.
    const isCurrent = effectiveOrderBy === column;
    const nextOrder =
      isCurrent && effectiveSortOrder === SortOrder.Descending ? SortOrder.Ascending : SortOrder.Descending;
    navigate({
      to: "/support/tickets",
      search: (previous) => ({
        ...previous,
        orderBy: column === DEFAULT_ORDER_BY ? undefined : column,
        sortOrder: nextOrder === DEFAULT_SORT_ORDER ? undefined : nextOrder,
        pageOffset: undefined,
        selectedTicketId: undefined
      })
    });
  };

  return (
    <SidebarProvider>
      <BackOfficeSideMenu />
      <SidebarInset>
        <AppLayout
          variant="center"
          maxWidth="80rem"
          browserTitle={t`Support tickets`}
          title={t`Support tickets`}
          subtitle={t`Across all accounts.`}
          sidePane={
            <BackOfficeSupportSidePane
              ticketId={selectedTicketId}
              onClose={() => handleSelectTicket(undefined)}
              mode="preview"
            />
          }
        >
          <InboxStatTiles counts={data?.counts} selectedStatus={status} onSelect={handleStatusChange} />

          <div className="mt-4">
            <InboxToolbar search={search} category={category} assignee={assignee} resultCount={data?.totalCount} />
          </div>

          {showEmpty ? (
            <InboxZeroEmpty />
          ) : showNoResults ? (
            <NoMatchesEmpty onClearFilters={handleClearFilters} />
          ) : (
            <div className="flex min-h-0 flex-1 flex-col">
              <InboxTable
                tickets={tickets}
                isLoading={isLoading}
                totalPages={data?.totalPages ?? 0}
                currentPageOffset={data?.currentPageOffset ?? 0}
                selectedTicketId={selectedTicketId}
                orderBy={effectiveOrderBy}
                sortOrder={effectiveSortOrder}
                onSelectTicket={handleSelectTicket}
                onPageChange={handlePageChange}
                onSort={handleSort}
              />
            </div>
          )}
        </AppLayout>
      </SidebarInset>
    </SidebarProvider>
  );
}
