import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { requireSubscriptionEnabled } from "@repo/infrastructure/auth/routeGuards";
import { AppLayout } from "@repo/ui/components/AppLayout";
import { Button } from "@repo/ui/components/Button";
import { Empty, EmptyContent, EmptyDescription, EmptyHeader, EmptyMedia, EmptyTitle } from "@repo/ui/components/Empty";
import { SidebarInset, SidebarProvider } from "@repo/ui/components/Sidebar";
import { keepPreviousData } from "@tanstack/react-query";
import { createFileRoute, useNavigate } from "@tanstack/react-router";
import { ReceiptIcon } from "lucide-react";
import { z } from "zod";

import { BackOfficeSideMenu } from "@/shared/components/BackOfficeSideMenu";
import {
  api,
  BackOfficeInvoiceStatusFilter,
  SortableBackOfficeInvoiceProperties,
  SortOrder
} from "@/shared/lib/api/client";

import { InvoicesTable } from "./-components/InvoicesTable";
import { InvoicesToolbar, type InvoicesView } from "./-components/InvoicesToolbar";

// Drives the URL-controlled split between all invoices, paid invoices, and refunds + credit notes.
// The backend filter accepts an open-ended Statuses[] array; the toggle maps the chosen view onto a
// fixed status set so operators don't have to think about which low-level statuses make up each
// business concept. "all" sends no status filter so every row is included.
const STATUSES_FOR_VIEW: Record<InvoicesView, BackOfficeInvoiceStatusFilter[] | undefined> = {
  all: undefined,
  invoices: [
    BackOfficeInvoiceStatusFilter.Paid,
    BackOfficeInvoiceStatusFilter.Pending,
    BackOfficeInvoiceStatusFilter.Failed
  ],
  refunds: [BackOfficeInvoiceStatusFilter.Refunded, BackOfficeInvoiceStatusFilter.HasCreditNote]
};

const invoicesSearchSchema = z.object({
  search: z.string().optional(),
  view: z.enum(["all", "invoices", "refunds"]).optional(),
  orderBy: z.nativeEnum(SortableBackOfficeInvoiceProperties).optional(),
  sortOrder: z.nativeEnum(SortOrder).optional(),
  pageOffset: z.number().int().nonnegative().optional()
});

export const Route = createFileRoute("/invoices/")({
  staticData: { trackingTitle: "Invoices" },
  validateSearch: invoicesSearchSchema,
  beforeLoad: () => requireSubscriptionEnabled(),
  component: InvoicesListPage
});

function InvoicesListPage() {
  const { search, view, orderBy, sortOrder, pageOffset } = Route.useSearch();
  const navigate = useNavigate();
  const activeView: InvoicesView = view ?? "all";

  const { data, isLoading } = api.useQuery(
    "get",
    "/api/back-office/invoices",
    {
      params: {
        query: {
          Search: search,
          Statuses: STATUSES_FOR_VIEW[activeView],
          OrderBy: orderBy,
          SortOrder: sortOrder,
          PageOffset: pageOffset
        }
      }
    },
    { placeholderData: keepPreviousData }
  );

  const invoices = data?.invoices ?? [];
  const hasSearch = Boolean(search);
  const showEmpty = !isLoading && invoices.length === 0;
  const subtitle =
    activeView === "refunds"
      ? t`Refunds and credit notes across all accounts.`
      : activeView === "invoices"
        ? t`Successful, pending, and failed invoices across all accounts.`
        : t`Every invoice, refund, and credit note across all accounts.`;

  return (
    <SidebarProvider>
      <BackOfficeSideMenu />
      <SidebarInset>
        <AppLayout variant="center" maxWidth="64rem" browserTitle={t`Invoices`} title={t`Invoices`} subtitle={subtitle}>
          <InvoicesToolbar search={search} view={activeView} />

          {showEmpty ? (
            <Empty>
              <EmptyHeader>
                <EmptyMedia variant="icon">
                  <ReceiptIcon />
                </EmptyMedia>
                <EmptyTitle>
                  {hasSearch ? (
                    <Trans>No results match your search</Trans>
                  ) : activeView === "refunds" ? (
                    <Trans>No refunds or credit notes yet</Trans>
                  ) : activeView === "invoices" ? (
                    <Trans>No invoices yet</Trans>
                  ) : (
                    <Trans>No records yet</Trans>
                  )}
                </EmptyTitle>
                <EmptyDescription>
                  {hasSearch ? (
                    <Trans>Try clearing the search to see more results.</Trans>
                  ) : (
                    <Trans>Records will appear here as accounts subscribe and Stripe webhooks are processed.</Trans>
                  )}
                </EmptyDescription>
              </EmptyHeader>
              {hasSearch && (
                <EmptyContent>
                  <Button variant="outline" size="sm" onClick={() => navigate({ to: "/invoices", search: () => ({}) })}>
                    <Trans>Clear filters</Trans>
                  </Button>
                </EmptyContent>
              )}
            </Empty>
          ) : (
            <div className="flex min-h-0 flex-1 flex-col">
              <InvoicesTable
                invoices={invoices}
                isLoading={isLoading}
                totalPages={data?.totalPages ?? 0}
                currentPageOffset={data?.currentPageOffset ?? 0}
                orderBy={orderBy}
                sortOrder={sortOrder}
              />
            </div>
          )}
        </AppLayout>
      </SidebarInset>
    </SidebarProvider>
  );
}
