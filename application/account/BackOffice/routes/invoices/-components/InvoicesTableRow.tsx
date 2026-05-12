import { Trans } from "@lingui/react/macro";
import { Badge } from "@repo/ui/components/Badge";
import { TableCell, TableRow } from "@repo/ui/components/Table";
import { TenantLogo } from "@repo/ui/components/TenantLogo";
import { useFormatDate } from "@repo/ui/hooks/useSmartDate";
import { formatCurrency } from "@repo/utils/currency/formatCurrency";

import type { components } from "@/shared/lib/api/client";

import { SmartDateTime } from "@/shared/components/SmartDateTime";
import { BackOfficeInvoiceRowKind, PaymentTransactionStatus } from "@/shared/lib/api/client";
import { getPaymentStatusLabel, getSubscriptionPlanLabel } from "@/shared/lib/api/labels";

type Invoice = components["schemas"]["BackOfficeInvoiceSummary"];

export function InvoicesTableRow({
  invoice,
  onRowClick
}: Readonly<{
  invoice: Invoice;
  onRowClick: (tenantId: string) => void;
}>) {
  const formatDate = useFormatDate();
  // Credit-note rows always show the amounts struck through to indicate the reversal of the underlying
  // invoice. Refunded invoice rows also strike through — the money came in then went back out.
  const isCreditNote = invoice.rowKind === BackOfficeInvoiceRowKind.CreditNote;
  const isRefunded = invoice.status === PaymentTransactionStatus.Refunded;
  const reverseAmounts = isCreditNote || isRefunded;
  const reverseClass = reverseAmounts ? "text-muted-foreground line-through" : "";

  return (
    <TableRow
      rowKey={`${invoice.id}-${invoice.rowKind}`}
      onClick={() => onRowClick(String(invoice.tenantId))}
      className="cursor-pointer"
    >
      <TableCell>
        <div className="flex min-w-0 items-center gap-3">
          <TenantLogo logoUrl={invoice.tenantLogoUrl} tenantName={invoice.tenantName} size="md" className="size-10" />
          <span className="truncate font-medium text-foreground">{invoice.tenantName}</span>
        </div>
      </TableCell>
      <TableCell className="whitespace-nowrap">
        <div className="flex flex-col leading-tight">
          <SmartDateTime date={invoice.date} />
          <span className="text-xs text-muted-foreground tabular-nums">{formatDate(invoice.date, true, true)}</span>
        </div>
      </TableCell>
      <TableCell className="hidden md:table-cell">
        {invoice.plan != null ? (
          <Badge variant="secondary">{getSubscriptionPlanLabel(invoice.plan)}</Badge>
        ) : (
          <span className="text-muted-foreground">—</span>
        )}
      </TableCell>
      <TableCell className={`hidden text-right whitespace-nowrap tabular-nums md:table-cell ${reverseClass}`}>
        {formatCurrency(invoice.amountExcludingTax, invoice.currency)}
      </TableCell>
      <TableCell
        className={`hidden text-right whitespace-nowrap text-muted-foreground tabular-nums xl:table-cell ${reverseAmounts ? "line-through" : ""}`}
      >
        {formatCurrency(invoice.taxAmount, invoice.currency)}
      </TableCell>
      <TableCell className={`text-right whitespace-nowrap tabular-nums ${reverseClass}`}>
        {formatCurrency(invoice.amount, invoice.currency)}
      </TableCell>
      <TableCell>
        <RowKindBadge rowKind={invoice.rowKind} status={invoice.status} failureReason={invoice.failureReason} />
      </TableCell>
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
