import type { PaymentTransaction } from "@repo/infrastructure/sync/hooks";

import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { trackInteraction } from "@repo/infrastructure/applicationInsights/ApplicationInsightsProvider";
import { Badge } from "@repo/ui/components/Badge";
import { buttonVariants } from "@repo/ui/components/Button";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@repo/ui/components/Table";
import { Tooltip, TooltipContent, TooltipTrigger } from "@repo/ui/components/Tooltip";
import { useFormatDate } from "@repo/ui/hooks/useSmartDate";
import { DownloadIcon } from "lucide-react";

function getStatusVariant(status: string): "default" | "secondary" | "destructive" | "outline" {
  switch (status) {
    case "Succeeded":
      return "default";
    case "Failed":
      return "destructive";
    case "Pending":
      return "outline";
    case "Refunded":
      return "secondary";
    default:
      return "outline";
  }
}

function getStatusLabel(status: string): string {
  switch (status) {
    case "Succeeded":
      return t`Succeeded`;
    case "Failed":
      return t`Failed`;
    case "Pending":
      return t`Pending`;
    case "Refunded":
      return t`Refunded`;
    default:
      return status;
  }
}

export function BillingHistoryTable({ transactions }: { transactions: PaymentTransaction[] }) {
  const formatDate = useFormatDate();

  if (transactions.length === 0) {
    return (
      <p className="text-sm text-muted-foreground">
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
                        <a
                          href={transaction.invoiceUrl}
                          target="_blank"
                          rel="noreferrer"
                          className={buttonVariants({ variant: "ghost", size: "sm" })}
                          aria-label={t`Invoice`}
                          onClick={() => trackInteraction("Billing history", "interaction", "Download invoice")}
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
                        <a
                          href={transaction.creditNoteUrl}
                          target="_blank"
                          rel="noreferrer"
                          className={buttonVariants({ variant: "ghost", size: "sm" })}
                          aria-label={t`Credit note`}
                          onClick={() => trackInteraction("Billing history", "interaction", "Download credit note")}
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
