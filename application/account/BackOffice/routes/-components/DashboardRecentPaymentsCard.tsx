import type { RowKey } from "@repo/ui/components/Table";

import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { Badge } from "@repo/ui/components/Badge";
import { Empty, EmptyDescription, EmptyHeader, EmptyTitle } from "@repo/ui/components/Empty";
import { Skeleton } from "@repo/ui/components/Skeleton";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@repo/ui/components/Table";
import { TenantLogo } from "@repo/ui/components/TenantLogo";
import { formatCurrency } from "@repo/utils/currency/formatCurrency";
import { Link, useNavigate } from "@tanstack/react-router";
import { ArrowRightIcon, ReceiptIcon } from "lucide-react";
import { useCallback } from "react";

import { SmartDateTime } from "@/shared/components/SmartDateTime";
import { api, PaymentTransactionStatus } from "@/shared/lib/api/client";
import { getPaymentStatusLabel, getSubscriptionPlanLabel } from "@/shared/lib/api/labels";

import { DashboardCardShell } from "./DashboardCardShell";

export function DashboardRecentPaymentsCard() {
  const navigate = useNavigate();
  const { data, isLoading } = api.useQuery("get", "/api/back-office/dashboard/recent-payments", {
    params: { query: { Limit: 6 } }
  });

  const payments = data?.payments ?? [];

  const handleActivate = useCallback(
    (key: RowKey) => {
      navigate({
        to: "/accounts/$tenantId",
        params: { tenantId: String(key).split("|")[0] },
        search: { tab: "invoices" }
      });
    },
    [navigate]
  );

  return (
    <DashboardCardShell
      title={<Trans>Recent payments</Trans>}
      action={
        <Link to="/invoices" className="flex items-center gap-1 text-sm text-muted-foreground hover:text-foreground">
          <Trans>View all</Trans>
          <ArrowRightIcon className="size-3.5" aria-hidden="true" />
        </Link>
      }
    >
      {isLoading ? (
        <div className="flex flex-col gap-3">
          {[0, 1, 2, 3, 4, 5].map((index) => (
            <Skeleton key={index} className="h-12 w-full" />
          ))}
        </div>
      ) : payments.length === 0 ? (
        <Empty className="border bg-card">
          <EmptyHeader>
            <ReceiptIcon className="size-6 text-muted-foreground" aria-hidden="true" />
            <EmptyTitle>
              <Trans>No recent payments</Trans>
            </EmptyTitle>
            <EmptyDescription>
              <Trans>Invoices will appear here as accounts subscribe and Stripe webhooks are processed.</Trans>
            </EmptyDescription>
          </EmptyHeader>
        </Empty>
      ) : (
        <Table
          rowSize="compact"
          aria-label={t`Recent payments`}
          selectionMode="single"
          onActivate={handleActivate}
          containerClassName="border-0 bg-transparent"
        >
          <TableHeader>
            <TableRow>
              <TableHead>
                <Trans>Account</Trans>
              </TableHead>
              <TableHead className="hidden md:table-cell">
                <Trans>Plan</Trans>
              </TableHead>
              <TableHead className="text-right">
                <Trans>Total</Trans>
              </TableHead>
              <TableHead>
                <Trans>Status</Trans>
              </TableHead>
              <TableHead className="hidden text-right md:table-cell">
                <Trans>Date</Trans>
              </TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {payments.map((payment) => {
              const isRefunded = payment.status === PaymentTransactionStatus.Refunded;
              return (
                <TableRow key={`${payment.tenantId}|${payment.id}`} rowKey={`${payment.tenantId}|${payment.id}`}>
                  <TableCell>
                    <div className="flex min-w-0 items-center gap-2">
                      <TenantLogo
                        logoUrl={payment.tenantLogoUrl}
                        tenantName={payment.tenantName}
                        size="md"
                        className="size-8 shrink-0"
                      />
                      <span className="truncate text-sm font-medium">{payment.tenantName}</span>
                    </div>
                  </TableCell>
                  <TableCell className="hidden md:table-cell">
                    {payment.plan != null ? (
                      <Badge variant="secondary">{getSubscriptionPlanLabel(payment.plan)}</Badge>
                    ) : (
                      <span className="text-muted-foreground">—</span>
                    )}
                  </TableCell>
                  <TableCell
                    className={`text-right whitespace-nowrap tabular-nums ${isRefunded ? "text-muted-foreground line-through" : ""}`}
                  >
                    {formatCurrency(payment.amount, payment.currency)}
                  </TableCell>
                  <TableCell>
                    <Badge
                      variant={payment.status === PaymentTransactionStatus.Failed ? "outline" : "secondary"}
                      className={
                        payment.status === PaymentTransactionStatus.Failed
                          ? "border-destructive/30 text-destructive"
                          : payment.status === PaymentTransactionStatus.Succeeded
                            ? "bg-success text-success-foreground"
                            : undefined
                      }
                    >
                      {getPaymentStatusLabel(payment.status)}
                    </Badge>
                  </TableCell>
                  <TableCell className="hidden text-right md:table-cell">
                    <SmartDateTime date={payment.date} className="text-xs whitespace-nowrap text-muted-foreground" />
                  </TableCell>
                </TableRow>
              );
            })}
          </TableBody>
        </Table>
      )}
    </DashboardCardShell>
  );
}
