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
import { InvoicesToolbar } from "./-components/InvoicesToolbar";

const invoicesSearchSchema = z.object({
  search: z.string().optional(),
  invoiceStatuses: z.array(z.nativeEnum(BackOfficeInvoiceStatusFilter)).max(10).optional(),
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
  const { search, invoiceStatuses, orderBy, sortOrder, pageOffset } = Route.useSearch();
  const navigate = useNavigate();

  const { data, isLoading } = api.useQuery(
    "get",
    "/api/back-office/invoices",
    {
      params: {
        query: {
          Search: search,
          Statuses: invoiceStatuses,
          OrderBy: orderBy,
          SortOrder: sortOrder,
          PageOffset: pageOffset
        }
      }
    },
    { placeholderData: keepPreviousData }
  );

  const invoices = data?.invoices ?? [];
  const hasFilters = Boolean(search) || (invoiceStatuses?.length ?? 0) > 0;
  const showEmpty = !isLoading && invoices.length === 0;

  return (
    <SidebarProvider>
      <BackOfficeSideMenu />
      <SidebarInset>
        <AppLayout
          variant="center"
          maxWidth="64rem"
          browserTitle={t`Invoices`}
          title={t`Invoices`}
          subtitle={t`Every invoice, refund, and credit note across all accounts.`}
        >
          <InvoicesToolbar search={search} invoiceStatuses={invoiceStatuses ?? []} />

          {showEmpty ? (
            <Empty>
              <EmptyHeader>
                <EmptyMedia variant="icon">
                  <ReceiptIcon />
                </EmptyMedia>
                <EmptyTitle>
                  {hasFilters ? <Trans>No invoices match your filters</Trans> : <Trans>No invoices yet</Trans>}
                </EmptyTitle>
                <EmptyDescription>
                  {hasFilters ? (
                    <Trans>Try clearing the search or filters to see more results.</Trans>
                  ) : (
                    <Trans>Invoices will appear here as accounts subscribe and Stripe webhooks are processed.</Trans>
                  )}
                </EmptyDescription>
              </EmptyHeader>
              {hasFilters && (
                <EmptyContent>
                  <Button
                    variant="outline"
                    size="sm"
                    onClick={() =>
                      navigate({
                        to: "/invoices",
                        search: () => ({})
                      })
                    }
                  >
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
