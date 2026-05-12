import type { ReactNode } from "react";

import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { Badge } from "@repo/ui/components/Badge";
import { Button } from "@repo/ui/components/Button";
import { TableCell, TableRow } from "@repo/ui/components/Table";
import { formatCurrency } from "@repo/utils/currency/formatCurrency";
import { DownloadIcon } from "lucide-react";

import type { components } from "@/shared/lib/api/client";

import { BackOfficeInvoiceRowKind, PaymentTransactionStatus } from "@/shared/lib/api/client";
import { getPaymentStatusLabel, getSubscriptionPlanLabel } from "@/shared/lib/api/labels";

type PaymentTransaction = components["schemas"]["TenantPaymentTransaction"];

export function AccountPaymentRow({
  transaction,
  renderDate,
  showPlan = true,
  showActions = true
}: Readonly<{
  transaction: PaymentTransaction;
  renderDate: (value: string | null | undefined) => ReactNode;
  showPlan?: boolean;
  showActions?: boolean;
}>) {
  // Strikethrough only on reversal rows (CreditNote, Refund). The Invoice row always carries the
  // original payment outcome and never gets strikethrough — the reversal lives in its own row.
  const isCreditNote = transaction.rowKind === BackOfficeInvoiceRowKind.CreditNote;
  const isRefundRow = transaction.rowKind === BackOfficeInvoiceRowKind.Refund;
  const isReversal = isCreditNote || isRefundRow;
  const reverseClass = isReversal ? "text-muted-foreground line-through" : "";
  return (
    <TableRow rowKey={`${transaction.id}-${transaction.rowKind}`}>
      <TableCell>
        <div className="flex flex-col leading-tight">{renderDate(transaction.date)}</div>
      </TableCell>
      {showPlan && (
        <TableCell className="hidden md:table-cell">
          {transaction.plan != null ? (
            <Badge variant="secondary">{getSubscriptionPlanLabel(transaction.plan)}</Badge>
          ) : (
            <span className="text-muted-foreground">—</span>
          )}
        </TableCell>
      )}
      <TableCell className={`hidden text-right whitespace-nowrap tabular-nums md:table-cell ${reverseClass}`}>
        {formatCurrency(transaction.amountExcludingTax, transaction.currency)}
      </TableCell>
      <TableCell
        className={`hidden text-right whitespace-nowrap text-muted-foreground tabular-nums md:table-cell ${isReversal ? "line-through" : ""}`}
      >
        {formatCurrency(transaction.taxAmount, transaction.currency)}
      </TableCell>
      <TableCell className={`text-right whitespace-nowrap tabular-nums ${reverseClass}`}>
        {formatCurrency(transaction.amount, transaction.currency)}
      </TableCell>
      <TableCell>
        <RowKindBadge
          rowKind={transaction.rowKind}
          status={transaction.status}
          failureReason={transaction.failureReason}
        />
      </TableCell>
      {showActions && (
        <TableCell className="text-right">
          <div className="flex items-center justify-end gap-2">
            {isCreditNote && transaction.creditNoteUrl && (
              <Button
                size="xs"
                variant="default"
                nativeButton={false}
                className="gap-1 max-sm:w-fit"
                render={
                  <a
                    href={transaction.creditNoteUrl}
                    target="_blank"
                    rel="noopener noreferrer"
                    aria-label={t`Open credit note`}
                  />
                }
              >
                <DownloadIcon className="size-3" />
                <span className="hidden md:inline">
                  <Trans>Credit note</Trans>
                </span>
              </Button>
            )}
            {!isCreditNote && !isRefundRow && transaction.invoiceUrl && (
              <Button
                size="xs"
                variant="default"
                nativeButton={false}
                className="gap-1 max-sm:w-fit"
                render={
                  <a
                    href={transaction.invoiceUrl}
                    target="_blank"
                    rel="noopener noreferrer"
                    aria-label={t`Open invoice`}
                  />
                }
              >
                <DownloadIcon className="size-3" />
                <span className="hidden md:inline">
                  <Trans>Invoice</Trans>
                </span>
              </Button>
            )}
          </div>
        </TableCell>
      )}
    </TableRow>
  );
}

function RowKindBadge({
  rowKind,
  status,
  failureReason
}: Readonly<{ rowKind: BackOfficeInvoiceRowKind; status: PaymentTransactionStatus; failureReason: string | null }>) {
  if (rowKind === BackOfficeInvoiceRowKind.CreditNote) {
    return (
      <Badge variant="secondary" className="text-muted-foreground">
        <Trans>Credit note</Trans>
      </Badge>
    );
  }

  if (rowKind === BackOfficeInvoiceRowKind.Refund) {
    return (
      <Badge variant="secondary" className="text-muted-foreground">
        <Trans>Refunded</Trans>
      </Badge>
    );
  }

  const variant = status === PaymentTransactionStatus.Failed ? "outline" : "secondary";
  const className =
    status === PaymentTransactionStatus.Failed
      ? "border-destructive/30 text-destructive"
      : status === PaymentTransactionStatus.Succeeded
        ? "bg-success text-success-foreground"
        : undefined;

  const badge = (
    <Badge variant={variant} className={className}>
      {getPaymentStatusLabel(status)}
    </Badge>
  );

  if (status === PaymentTransactionStatus.Failed && failureReason) {
    return (
      <div className="flex flex-col gap-0.5">
        {badge}
        <span className="text-xs text-muted-foreground">{failureReason}</span>
      </div>
    );
  }

  return badge;
}
