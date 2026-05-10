import type { ReactNode } from "react";

import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { Badge } from "@repo/ui/components/Badge";
import { Button } from "@repo/ui/components/Button";
import { TableCell, TableRow } from "@repo/ui/components/Table";
import { formatCurrency } from "@repo/utils/currency/formatCurrency";
import { DownloadIcon } from "lucide-react";

import type { components } from "@/shared/lib/api/client";

import { PaymentTransactionStatus } from "@/shared/lib/api/client";
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
  // Refunded rows show the amounts struck through — money came in, then went back out.
  const isRefunded = transaction.status === PaymentTransactionStatus.Refunded;
  const refundedClass = isRefunded ? "text-muted-foreground line-through" : "";
  return (
    <TableRow rowKey={transaction.id}>
      <TableCell>{renderDate(transaction.date)}</TableCell>
      {showPlan && (
        <TableCell className="hidden md:table-cell">
          {transaction.plan != null ? (
            <Badge variant="secondary">{getSubscriptionPlanLabel(transaction.plan)}</Badge>
          ) : (
            <span className="text-muted-foreground">—</span>
          )}
        </TableCell>
      )}
      <TableCell className={`hidden text-right whitespace-nowrap tabular-nums md:table-cell ${refundedClass}`}>
        {formatCurrency(transaction.amountExcludingTax, transaction.currency)}
      </TableCell>
      <TableCell
        className={`hidden text-right whitespace-nowrap text-muted-foreground tabular-nums md:table-cell ${isRefunded ? "line-through" : ""}`}
      >
        {formatCurrency(transaction.taxAmount, transaction.currency)}
      </TableCell>
      <TableCell className={`text-right whitespace-nowrap tabular-nums ${refundedClass}`}>
        {formatCurrency(transaction.amount, transaction.currency)}
      </TableCell>
      <TableCell>
        <PaymentStatusBadge status={transaction.status} failureReason={transaction.failureReason} />
      </TableCell>
      {showActions && (
        <TableCell className="text-right">
          <div className="flex items-center justify-end gap-2">
            {transaction.invoiceUrl && (
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
            {transaction.creditNoteUrl && (
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
          </div>
        </TableCell>
      )}
    </TableRow>
  );
}

function PaymentStatusBadge({
  status,
  failureReason
}: Readonly<{ status: PaymentTransactionStatus; failureReason: string | null }>) {
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
