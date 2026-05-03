import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { Empty, EmptyDescription, EmptyHeader, EmptyTitle } from "@repo/ui/components/Empty";
import { Skeleton } from "@repo/ui/components/Skeleton";
import { Table, TableBody, TableHead, TableHeader, TableRow } from "@repo/ui/components/Table";
import { TablePagination } from "@repo/ui/components/TablePagination";
import { useFormatDate } from "@repo/ui/hooks/useSmartDate";
import { keepPreviousData } from "@tanstack/react-query";
import { useState } from "react";

import type { components } from "@/shared/lib/api/client";

import { api } from "@/shared/lib/api/client";

import { AccountPaymentRow } from "./AccountPaymentRow";

type TenantDetailResponse = components["schemas"]["TenantDetailResponse"];

interface AccountBillingTabProps {
  tenant: TenantDetailResponse | undefined;
  tenantId: string;
}

export function AccountBillingTab({ tenant, tenantId }: Readonly<AccountBillingTabProps>) {
  const formatDate = useFormatDate();
  const [pageOffset, setPageOffset] = useState(0);

  const { data, isLoading } = api.useQuery(
    "get",
    "/api/back-office/tenants/{id}/payment-history",
    {
      params: { path: { id: tenantId }, query: { PageOffset: pageOffset || undefined } }
    },
    { placeholderData: keepPreviousData }
  );

  const transactions = data?.transactions ?? [];
  const totalPages = data?.totalPages ?? 0;
  const currentPage = (data?.currentPageOffset ?? 0) + 1;

  return (
    <div className="grid grid-cols-1 gap-6 lg:grid-cols-3">
      <section className="lg:col-span-1">
        <h3 className="mb-3 text-base font-semibold">
          <Trans>Billing address</Trans>
        </h3>
        {!tenant ? (
          <Skeleton className="h-32 w-full" />
        ) : tenant.billingAddress ? (
          <BillingAddress address={tenant.billingAddress} />
        ) : (
          <Empty className="border">
            <EmptyHeader>
              <EmptyTitle>
                <Trans>No billing address</Trans>
              </EmptyTitle>
              <EmptyDescription>
                <Trans>No billing address on file.</Trans>
              </EmptyDescription>
            </EmptyHeader>
          </Empty>
        )}
      </section>

      <section className="lg:col-span-2">
        <h3 className="mb-3 text-base font-semibold">
          <Trans>Payment history</Trans>
        </h3>
        {isLoading && transactions.length === 0 ? (
          <div className="flex flex-col gap-2">
            {Array.from({ length: 5 }).map((_, index) => (
              <Skeleton key={`payment-skeleton-${index}`} className="h-12 w-full" />
            ))}
          </div>
        ) : transactions.length === 0 ? (
          <Empty className="border">
            <EmptyHeader>
              <EmptyTitle>
                <Trans>No payments</Trans>
              </EmptyTitle>
              <EmptyDescription>
                <Trans>No payments yet.</Trans>
              </EmptyDescription>
            </EmptyHeader>
          </Empty>
        ) : (
          <div className="overflow-hidden rounded-md border border-border">
            <Table rowSize="compact" aria-label={t`Payment history`}>
              <TableHeader>
                <TableRow>
                  <TableHead>
                    <Trans>Date</Trans>
                  </TableHead>
                  <TableHead>
                    <Trans>Amount</Trans>
                  </TableHead>
                  <TableHead>
                    <Trans>Status</Trans>
                  </TableHead>
                  <TableHead className="text-right">
                    <Trans>Documents</Trans>
                  </TableHead>
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

        {totalPages > 1 && (
          <div className="pt-4">
            <TablePagination
              currentPage={currentPage}
              totalPages={totalPages}
              onPageChange={(page) => setPageOffset(page - 1)}
              previousLabel={t`Previous`}
              nextLabel={t`Next`}
              trackingTitle="Payment history"
              className="w-full"
            />
          </div>
        )}
      </section>
    </div>
  );
}

function BillingAddress({ address }: Readonly<{ address: components["schemas"]["BillingAddressResponse"] }>) {
  const lines = [
    address.line1,
    address.line2,
    [address.postalCode, address.city].filter(Boolean).join(" ").trim() || null,
    address.state,
    address.country
  ].filter((value): value is string => Boolean(value && value.trim().length > 0));

  if (lines.length === 0) {
    return (
      <Empty className="border">
        <EmptyHeader>
          <EmptyTitle>
            <Trans>No billing address</Trans>
          </EmptyTitle>
          <EmptyDescription>
            <Trans>No billing address on file.</Trans>
          </EmptyDescription>
        </EmptyHeader>
      </Empty>
    );
  }

  return (
    <address className="rounded-md border border-border bg-card p-4 text-sm leading-6 not-italic">
      {lines.map((line) => (
        <div key={line}>{line}</div>
      ))}
    </address>
  );
}
