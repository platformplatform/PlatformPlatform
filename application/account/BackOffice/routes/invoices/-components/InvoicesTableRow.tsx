import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { Badge } from "@repo/ui/components/Badge";
import { Button } from "@repo/ui/components/Button";
import { TableCell, TableRow } from "@repo/ui/components/Table";
import { TenantLogo } from "@repo/ui/components/TenantLogo";
import { formatCurrency } from "@repo/utils/currency/formatCurrency";
import { DownloadIcon } from "lucide-react";

import type { components } from "@/shared/lib/api/client";

import { PaymentTransactionStatus } from "@/shared/lib/api/client";
import { getPaymentStatusLabel, getSubscriptionPlanLabel } from "@/shared/lib/api/labels";

type Invoice = components["schemas"]["BackOfficeInvoiceSummary"];

export function InvoicesTableRow({
  invoice,
  formatDate,
  onRowClick
}: Readonly<{
  invoice: Invoice;
  formatDate: (value: string | null | undefined) => string;
  onRowClick: (tenantId: string) => void;
}>) {
  // Refunded rows show the amounts struck through — money came in, then went back out. Mirrors the
  // per-account Invoices tab so operators see consistent semantics across both surfaces.
  const isRefunded = invoice.status === PaymentTransactionStatus.Refunded;
  const refundedClass = isRefunded ? "text-muted-foreground line-through" : "";

  return (
    <TableRow rowKey={invoice.id} onClick={() => onRowClick(String(invoice.tenantId))} className="cursor-pointer">
      <TableCell>
        <div className="flex min-w-0 items-center gap-3">
          <TenantLogo logoUrl={invoice.tenantLogoUrl} tenantName={invoice.tenantName} size="md" className="size-10" />
          <span className="truncate font-medium text-foreground">{invoice.tenantName}</span>
        </div>
      </TableCell>
      <TableCell className="text-sm whitespace-nowrap text-muted-foreground tabular-nums">
        {formatDate(invoice.date)}
      </TableCell>
      <TableCell className="hidden md:table-cell">
        {invoice.plan != null ? (
          <Badge variant="secondary">{getSubscriptionPlanLabel(invoice.plan)}</Badge>
        ) : (
          <span className="text-muted-foreground">—</span>
        )}
      </TableCell>
      <TableCell className={`hidden text-right whitespace-nowrap tabular-nums md:table-cell ${refundedClass}`}>
        {formatCurrency(invoice.amountExcludingTax, invoice.currency)}
      </TableCell>
      <TableCell
        className={`hidden text-right whitespace-nowrap text-muted-foreground tabular-nums xl:table-cell ${isRefunded ? "line-through" : ""}`}
      >
        {formatCurrency(invoice.taxAmount, invoice.currency)}
      </TableCell>
      <TableCell className={`text-right whitespace-nowrap tabular-nums ${refundedClass}`}>
        {formatCurrency(invoice.amount, invoice.currency)}
      </TableCell>
      <TableCell>
        <PaymentStatusBadge status={invoice.status} failureReason={invoice.failureReason} />
      </TableCell>
      <TableCell className="text-right" onClick={(event) => event.stopPropagation()}>
        <div className="flex items-center justify-end gap-2">
          {invoice.invoiceUrl && (
            <Button
              size="xs"
              variant="default"
              nativeButton={false}
              className="gap-1 max-sm:w-fit"
              render={
                <a href={invoice.invoiceUrl} target="_blank" rel="noopener noreferrer" aria-label={t`Open invoice`} />
              }
            >
              <DownloadIcon className="size-3" />
              <span className="hidden md:inline">
                <Trans>Invoice</Trans>
              </span>
            </Button>
          )}
          {invoice.creditNoteUrl && (
            <Button
              size="xs"
              variant="default"
              nativeButton={false}
              className="gap-1 max-sm:w-fit"
              render={
                <a
                  href={invoice.creditNoteUrl}
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
