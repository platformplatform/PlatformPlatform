import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { Badge } from "@repo/ui/components/Badge";
import { Button } from "@repo/ui/components/Button";
import { TableCell, TableRow } from "@repo/ui/components/Table";
import { formatCurrency } from "@repo/utils/currency/formatCurrency";
import { ExternalLinkIcon } from "lucide-react";

import type { components } from "@/shared/lib/api/client";

import { PaymentTransactionStatus } from "@/shared/lib/api/client";
import { getPaymentStatusLabel } from "@/shared/lib/api/labels";

type PaymentTransaction = components["schemas"]["TenantPaymentTransaction"];

export function AccountPaymentRow({
  transaction,
  formatDate
}: Readonly<{
  transaction: PaymentTransaction;
  formatDate: (value: string | null | undefined) => string;
}>) {
  return (
    <TableRow rowKey={transaction.id}>
      <TableCell>{formatDate(transaction.date)}</TableCell>
      <TableCell className="tabular-nums">{formatCurrency(transaction.amount, transaction.currency)}</TableCell>
      <TableCell>
        <PaymentStatusBadge status={transaction.status} failureReason={transaction.failureReason} />
      </TableCell>
      <TableCell className="text-right">
        <div className="flex justify-end gap-2">
          {transaction.invoiceUrl && (
            <Button
              size="sm"
              variant="outline"
              className="gap-1"
              render={
                <a
                  href={transaction.invoiceUrl}
                  target="_blank"
                  rel="noopener noreferrer"
                  aria-label={t`Open invoice`}
                />
              }
            >
              <ExternalLinkIcon className="size-3" />
              <Trans>Invoice</Trans>
            </Button>
          )}
          {transaction.creditNoteUrl && (
            <Button
              size="sm"
              variant="outline"
              className="gap-1"
              render={
                <a
                  href={transaction.creditNoteUrl}
                  target="_blank"
                  rel="noopener noreferrer"
                  aria-label={t`Open credit note`}
                />
              }
            >
              <ExternalLinkIcon className="size-3" />
              <Trans>Credit note</Trans>
            </Button>
          )}
        </div>
      </TableCell>
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
