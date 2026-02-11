import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { Badge } from "@repo/ui/components/Badge";
import { buttonVariants } from "@repo/ui/components/Button";
import { Link } from "@repo/ui/components/Link";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@repo/ui/components/Table";
import { useFormatDate } from "@repo/ui/hooks/useSmartDate";
import { DownloadIcon } from "lucide-react";
import { api, PaymentTransactionStatus } from "@/shared/lib/api/client";

const PAGE_SIZE = 10;

function getStatusVariant(status: PaymentTransactionStatus): "default" | "secondary" | "destructive" | "outline" {
  switch (status) {
    case PaymentTransactionStatus.Succeeded:
      return "default";
    case PaymentTransactionStatus.Failed:
      return "destructive";
    case PaymentTransactionStatus.Pending:
      return "outline";
    case PaymentTransactionStatus.Refunded:
      return "secondary";
  }
}

function getStatusLabel(status: PaymentTransactionStatus): string {
  switch (status) {
    case PaymentTransactionStatus.Succeeded:
      return t`Succeeded`;
    case PaymentTransactionStatus.Failed:
      return t`Failed`;
    case PaymentTransactionStatus.Pending:
      return t`Pending`;
    case PaymentTransactionStatus.Refunded:
      return t`Refunded`;
  }
}

export function BillingHistoryTable() {
  const formatDate = useFormatDate();
  const { data, isLoading } = api.useQuery("get", "/api/account/subscriptions/payment-history", {
    params: { query: { PageOffset: 0, PageSize: PAGE_SIZE } }
  });

  const transactions = data?.transactions ?? [];

  if (isLoading) {
    return null;
  }

  if (transactions.length === 0) {
    return (
      <p className="text-muted-foreground text-sm">
        <Trans>No payment history available.</Trans>
      </p>
    );
  }

  return (
    <Table>
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
          <TableHead />
        </TableRow>
      </TableHeader>
      <TableBody>
        {transactions.map((transaction) => (
          <TableRow key={transaction.id}>
            <TableCell>{formatDate(transaction.date)}</TableCell>
            <TableCell>
              {new Intl.NumberFormat(undefined, {
                style: "currency",
                currency: transaction.currency.toUpperCase()
              }).format(transaction.amount)}
            </TableCell>
            <TableCell>
              <Badge variant={getStatusVariant(transaction.status)}>{getStatusLabel(transaction.status)}</Badge>
            </TableCell>
            <TableCell>
              {transaction.invoiceUrl && (
                <Link
                  href={transaction.invoiceUrl}
                  target="_blank"
                  rel="noreferrer"
                  className={buttonVariants({ variant: "ghost", size: "sm" })}
                >
                  <DownloadIcon className="size-4" />
                  <Trans>Invoice</Trans>
                </Link>
              )}
            </TableCell>
          </TableRow>
        ))}
      </TableBody>
    </Table>
  );
}
