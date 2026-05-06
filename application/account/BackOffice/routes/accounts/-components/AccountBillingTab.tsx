import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { Empty, EmptyDescription, EmptyHeader, EmptyTitle } from "@repo/ui/components/Empty";
import { Skeleton } from "@repo/ui/components/Skeleton";
import { Table, TableBody, TableHead, TableHeader, TableRow } from "@repo/ui/components/Table";
import { TablePagination } from "@repo/ui/components/TablePagination";
import { useFormatDate } from "@repo/ui/hooks/useSmartDate";
import { keepPreviousData } from "@tanstack/react-query";
import { useState } from "react";

import { api } from "@/shared/lib/api/client";

import { AccountBillingHistorySection } from "./AccountBillingHistorySection";
import { AccountPaymentRow } from "./AccountPaymentRow";

interface AccountBillingTabProps {
  tenantId: string;
  /**
   * `compact` — Overview tab: show only the last 2 events and the last invoice (no pagination).
   * `full` — Billing tab: full pageable list of events and invoices.
   */
  variant?: "compact" | "full";
}

export function AccountBillingTab({ tenantId, variant = "compact" }: Readonly<AccountBillingTabProps>) {
  const formatDate = useFormatDate();
  const [pageOffset, setPageOffset] = useState(0);

  const isCompact = variant === "compact";
  const paymentsPageSize = isCompact ? 1 : 25;
  const eventsPageSize = isCompact ? 5 : 50;
  const eventsMaxItems = isCompact ? 2 : undefined;

  const { data, isLoading } = api.useQuery(
    "get",
    "/api/back-office/tenants/{id}/payment-history",
    {
      params: {
        path: { id: tenantId },
        query: { PageOffset: pageOffset || undefined, PageSize: paymentsPageSize }
      }
    },
    { placeholderData: keepPreviousData }
  );

  const eventsQuery = api.useQuery(
    "get",
    "/api/back-office/billing-events",
    {
      params: { query: { TenantId: tenantId, PageSize: eventsPageSize } }
    },
    { placeholderData: keepPreviousData }
  );

  const transactions = data?.transactions ?? [];
  const totalPages = data?.totalPages ?? 0;
  const currentPage = (data?.currentPageOffset ?? 0) + 1;
  const billingEvents = eventsQuery.data?.billingEvents ?? [];

  return (
    <section className="flex h-full flex-col gap-6">
      <div className="flex flex-col">
        <h4 className="mb-3">
          <Trans>Billing events</Trans>
        </h4>
        <AccountBillingHistorySection
          events={billingEvents}
          isLoading={eventsQuery.isLoading}
          maxItems={eventsMaxItems}
        />
      </div>

      <div className="flex min-h-0 flex-1 flex-col">
        <h4 className="mb-3">
          <Trans>Billing history</Trans>
        </h4>
        {isLoading && transactions.length === 0 ? (
          <div className="flex flex-col gap-2 rounded-lg border border-border bg-card p-2">
            {Array.from({ length: isCompact ? 1 : 5 }).map((_, index) => (
              <Skeleton key={`payment-skeleton-${index}`} className="h-12 w-full" />
            ))}
          </div>
        ) : transactions.length === 0 ? (
          <Empty className="border bg-card">
            <EmptyHeader>
              <EmptyTitle>
                <Trans>No invoices</Trans>
              </EmptyTitle>
              <EmptyDescription>
                <Trans>No invoices yet.</Trans>
              </EmptyDescription>
            </EmptyHeader>
          </Empty>
        ) : (
          <div className="flex min-h-0 flex-1 overflow-y-auto">
            <Table rowSize="compact" aria-label={t`Billing history`} stickyHeader={true}>
              <TableHeader>
                <TableRow>
                  <TableHead>
                    <Trans>Date</Trans>
                  </TableHead>
                  <TableHead>
                    <Trans>Plan</Trans>
                  </TableHead>
                  <TableHead>
                    <Trans>Amount</Trans>
                  </TableHead>
                  <TableHead>
                    <Trans>Status</Trans>
                  </TableHead>
                  <TableHead className="text-right" />
                </TableRow>
              </TableHeader>
              <TableBody>
                {transactions.map((transaction) => (
                  <AccountPaymentRow key={transaction.id} transaction={transaction} formatDate={formatDate} />
                ))}
              </TableBody>
            </Table>
          </div>
        )}

        {!isCompact && totalPages > 1 && (
          <div className="pt-4">
            <TablePagination
              currentPage={currentPage}
              totalPages={totalPages}
              onPageChange={(page) => setPageOffset(page - 1)}
              previousLabel={t`Previous`}
              nextLabel={t`Next`}
              trackingTitle="Billing history"
              className="w-full"
            />
          </div>
        )}
      </div>
    </section>
  );
}
