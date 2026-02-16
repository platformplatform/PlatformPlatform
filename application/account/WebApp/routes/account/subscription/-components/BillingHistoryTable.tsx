import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { Badge } from "@repo/ui/components/Badge";
import { buttonVariants } from "@repo/ui/components/Button";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@repo/ui/components/Table";
import { Tooltip, TooltipContent, TooltipTrigger } from "@repo/ui/components/Tooltip";
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
          <TableHead className="w-1/3">
            <Trans>Date</Trans>
          </TableHead>
          <TableHead className="w-1/3">
            <Trans>Amount</Trans>
          </TableHead>
          <TableHead className="w-1/3">
            <Trans>Status</Trans>
          </TableHead>
          <TableHead className="w-px text-right" />
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
            <TableCell className="text-right">
              <div className="flex justify-end gap-1">
                {transaction.invoiceUrl && (
                  <Tooltip>
                    <TooltipTrigger
                      render={
                        // biome-ignore lint/a11y/useAnchorContent: Children injected by TooltipTrigger render prop; aria-label provides accessible name
                        <a
                          href={transaction.invoiceUrl}
                          target="_blank"
                          rel="noreferrer"
                          className={buttonVariants({ variant: "ghost", size: "sm" })}
                          aria-label={t`Invoice`}
                        />
                      }
                    >
                      <DownloadIcon className="size-4" />
                      <span className="hidden sm:inline" aria-hidden="true">
                        <Trans>Invoice</Trans>
                      </span>
                    </TooltipTrigger>
                    <TooltipContent className="sm:hidden">
                      <Trans>Invoice</Trans>
                    </TooltipContent>
                  </Tooltip>
                )}
                {transaction.creditNoteUrl && (
                  <Tooltip>
                    <TooltipTrigger
                      render={
                        // biome-ignore lint/a11y/useAnchorContent: Children injected by TooltipTrigger render prop; aria-label provides accessible name
                        <a
                          href={transaction.creditNoteUrl}
                          target="_blank"
                          rel="noreferrer"
                          className={buttonVariants({ variant: "ghost", size: "sm" })}
                          aria-label={t`Credit note`}
                        />
                      }
                    >
                      <DownloadIcon className="size-4" />
                      <span className="hidden sm:inline" aria-hidden="true">
                        <Trans>Credit note</Trans>
                      </span>
                    </TooltipTrigger>
                    <TooltipContent className="sm:hidden">
                      <Trans>Credit note</Trans>
                    </TooltipContent>
                  </Tooltip>
                )}
              </div>
            </TableCell>
          </TableRow>
        ))}
      </TableBody>
    </Table>
  );
}
