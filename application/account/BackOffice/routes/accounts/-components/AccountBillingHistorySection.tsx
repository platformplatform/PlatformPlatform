import type { ReactNode } from "react";

import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { Button } from "@repo/ui/components/Button";
import { Empty, EmptyDescription, EmptyHeader, EmptyTitle } from "@repo/ui/components/Empty";
import { Skeleton } from "@repo/ui/components/Skeleton";
import { Table, TableBody, TableHead, TableHeader, TableRow } from "@repo/ui/components/Table";
import { TablePagination } from "@repo/ui/components/TablePagination";
import { ArrowRightIcon } from "lucide-react";

import type { components } from "@/shared/lib/api/client";

import { AccountPaymentRow } from "./AccountPaymentRow";

type PaymentTransaction = components["schemas"]["TenantPaymentTransaction"];
type RenderDate = (value: string | null | undefined) => ReactNode;

interface Props {
  transactions: PaymentTransaction[];
  isLoading: boolean;
  isCompact: boolean;
  totalTransactions: number;
  totalPages: number;
  currentPage: number;
  onViewAll?: () => void;
  onPageChange: (offset: number) => void;
  renderDate: RenderDate;
}

export function AccountBillingHistorySection({
  transactions,
  isLoading,
  isCompact,
  totalTransactions,
  totalPages,
  currentPage,
  onViewAll,
  onPageChange,
  renderDate
}: Readonly<Props>) {
  return (
    <div className="flex flex-col">
      {isCompact ? (
        <div className="mb-3 flex items-baseline justify-between gap-3">
          <h4 className="whitespace-nowrap">
            <Trans>Invoices</Trans>
          </h4>
          {onViewAll && totalTransactions > 0 && (
            <Button
              variant="ghost"
              size="xs"
              onClick={onViewAll}
              className="ml-auto text-sm whitespace-nowrap text-muted-foreground hover:text-foreground max-sm:w-fit"
            >
              <Trans>View all {totalTransactions} invoices</Trans>
              <ArrowRightIcon className="size-3.5" aria-hidden={true} />
            </Button>
          )}
        </div>
      ) : (
        <div className="mb-3 text-sm text-muted-foreground">
          <Trans>Every invoice, refund, and credit note — the money in and out for this subscription.</Trans>
        </div>
      )}
      {isLoading && transactions.length === 0 ? (
        <div className="flex flex-col gap-2 rounded-lg border border-border bg-card p-2">
          {Array.from({ length: isCompact ? 2 : 5 }).map((_, index) => (
            <Skeleton key={`payment-skeleton-${index}`} className="h-12 w-full" />
          ))}
        </div>
      ) : transactions.length === 0 ? (
        <Empty className="h-[8.375rem] flex-none border bg-card p-4 md:p-4">
          <EmptyHeader>
            <EmptyTitle>
              <Trans>No transactions</Trans>
            </EmptyTitle>
            <EmptyDescription>
              <Trans>No invoices, refunds, or credit notes yet.</Trans>
            </EmptyDescription>
          </EmptyHeader>
        </Empty>
      ) : (
        <Table rowSize="compact" aria-label={t`Invoices`} stickyHeader={true}>
          <TableHeader>
            <TableRow>
              <TableHead>
                <Trans>Date</Trans>
              </TableHead>
              {!isCompact && (
                <TableHead className="hidden md:table-cell">
                  <Trans>Plan</Trans>
                </TableHead>
              )}
              <TableHead className="hidden text-right md:table-cell">
                <Trans>Amount</Trans>
              </TableHead>
              <TableHead className="hidden text-right md:table-cell">
                <Trans>VAT</Trans>
              </TableHead>
              <TableHead className="text-right">
                <Trans>Total</Trans>
              </TableHead>
              <TableHead>
                <Trans>Status</Trans>
              </TableHead>
              {!isCompact && (
                <TableHead className="text-right">
                  <span className="sr-only">
                    <Trans>Actions</Trans>
                  </span>
                </TableHead>
              )}
            </TableRow>
          </TableHeader>
          <TableBody>
            {transactions.map((transaction) => (
              <AccountPaymentRow
                key={`${transaction.id}-${transaction.rowKind}`}
                transaction={transaction}
                renderDate={renderDate}
                showPlan={!isCompact}
                showActions={!isCompact}
              />
            ))}
          </TableBody>
        </Table>
      )}

      {!isCompact && totalPages > 1 && (
        <div className="pt-4">
          <TablePagination
            currentPage={currentPage}
            totalPages={totalPages}
            onPageChange={(page) => onPageChange(page - 1)}
            previousLabel={t`Previous`}
            nextLabel={t`Next`}
            trackingTitle="Billing history"
            className="w-full"
          />
        </div>
      )}
    </div>
  );
}
