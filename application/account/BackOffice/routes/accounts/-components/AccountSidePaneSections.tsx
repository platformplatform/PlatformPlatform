import { t } from "@lingui/core/macro";
import { useLingui } from "@lingui/react";
import { Skeleton } from "@repo/ui/components/Skeleton";
import { useFormatDate } from "@repo/ui/hooks/useSmartDate";
import { formatCurrency } from "@repo/utils/currency/formatCurrency";

import type { components } from "@/shared/lib/api/client";

import { api, PlannedSubscriptionChange, UserRole } from "@/shared/lib/api/client";
import { formatRelativeTime } from "@/shared/lib/relativeTime";

import { SidePaneDivider, SidePaneSection } from "./SidePaneSection";
import { SidePaneUserList } from "./SidePaneUserList";
import { SidePaneUsersRow } from "./SidePaneUsersRow";
import { SubscriptionStatusIndicator } from "./SubscriptionStatusIndicator";

type TenantSummary = components["schemas"]["TenantSummary"];
type TenantDetailResponse = components["schemas"]["TenantDetailResponse"];

interface AccountSidePaneSectionsProps {
  tenant: TenantSummary;
  detail: TenantDetailResponse | null;
  detailLoading: boolean;
  debouncedTenantId: string;
  detailReady: boolean;
}

function formatAmount(amount: number | null | undefined, currency: string | null | undefined): string {
  if (amount === null || amount === undefined || currency === null || currency === undefined) {
    return "-";
  }
  return formatCurrency(amount, currency);
}

export function AccountSidePaneSections({
  tenant,
  detail,
  detailLoading,
  debouncedTenantId,
  detailReady
}: Readonly<AccountSidePaneSectionsProps>) {
  const formatDate = useFormatDate();
  const { i18n } = useLingui();

  const userCountsQuery = api.useQuery(
    "get",
    "/api/back-office/tenants/{id}/user-counts",
    { params: { path: { id: debouncedTenantId } } },
    { enabled: detailReady }
  );

  const ownersQuery = api.useQuery(
    "get",
    "/api/back-office/tenants/{id}/users",
    { params: { path: { id: debouncedTenantId }, query: { Roles: [UserRole.Owner], PageSize: 100 } } },
    { enabled: detailReady }
  );

  const paymentHistoryQuery = api.useQuery(
    "get",
    "/api/back-office/tenants/{id}/payment-history",
    { params: { path: { id: debouncedTenantId }, query: { PageSize: 1 } } },
    { enabled: detailReady }
  );

  const lastInvoice = paymentHistoryQuery.data?.transactions[0] ?? null;
  const paymentHistoryLoading = !detailReady || paymentHistoryQuery.isLoading;
  const subscribedSince = detail?.subscribedSince ?? null;

  const isCanceling = tenant.plannedChange === PlannedSubscriptionChange.Cancellation;
  const isDowngrading = tenant.plannedChange === PlannedSubscriptionChange.ScheduledPlanChange;
  const isCanceled = tenant.plan === "Basis" && tenant.hasEverSubscribed && !isCanceling && !isDowngrading;
  const newMrrAmount = isCanceling ? 0 : isDowngrading ? (detail?.scheduledPriceAmount ?? null) : null;
  const showStrikedMrr = (isCanceling || isDowngrading) && newMrrAmount !== null;

  return (
    <div className="flex flex-col">
      <SidePaneDivider />

      <SidePaneSection label={t`Plan & revenue`} className="h-[12.875rem]">
        <div className="flex flex-col gap-3">
          <KpiRow
            leftLabel={isCanceled ? t`Expired` : isCanceling ? t`Expires` : t`Renewal date`}
            leftValue={tenant.renewalDate ? formatDate(tenant.renewalDate) : "-"}
            rightLabel={t`MRR`}
            rightValue={
              showStrikedMrr ? (
                <span className="flex items-baseline justify-end gap-1">
                  <span className="text-muted-foreground line-through">
                    {formatAmount(tenant.monthlyRecurringRevenue, tenant.currency)}
                  </span>
                  <span aria-hidden={true} className="text-muted-foreground">
                    →
                  </span>
                  <span>{formatAmount(newMrrAmount, tenant.currency)}</span>
                </span>
              ) : (
                formatAmount(tenant.monthlyRecurringRevenue, tenant.currency)
              )
            }
          />

          <KpiRow
            leftLabel={t`Subscribed since`}
            leftValue={subscribedSince ? formatDate(subscribedSince) : "-"}
            leftLoading={detailLoading}
            rightLabel={t`Lifetime value`}
            rightValue={detail ? formatAmount(detail.lifetimeValue, detail.currency) : "-"}
            rightLoading={detailLoading}
          />

          <KpiRow
            leftLabel={t`Last invoice`}
            leftValue={lastInvoice ? formatDate(lastInvoice.date) : "-"}
            leftLoading={paymentHistoryLoading}
            rightLabel={t`Subtotal`}
            rightValue={lastInvoice ? formatAmount(lastInvoice.amountExcludingTax, lastInvoice.currency) : "-"}
            rightLoading={paymentHistoryLoading}
          />

          <SubscriptionStatusIndicator state={detail?.state} />
        </div>
      </SidePaneSection>

      <SidePaneDivider />

      <SidePaneSection label={t`Owners`} className="min-h-[7.25rem]">
        <SidePaneUserList
          users={ownersQuery.data?.users ?? []}
          isLoading={!detailReady || ownersQuery.isLoading}
          emptyMessage={t`No owners on this account.`}
        />
      </SidePaneSection>

      <SidePaneDivider />

      <SidePaneSection label={t`Users`} className="min-h-[6.375rem]">
        <SidePaneUsersRow
          detailReady={detailReady}
          userCounts={userCountsQuery.data}
          isLoading={userCountsQuery.isLoading}
        />
      </SidePaneSection>

      <SidePaneDivider />

      <SidePaneSection label={t`Signed up`}>
        <span className="flex items-center justify-between gap-2 text-sm">
          <span>{formatDate(tenant.createdAt)}</span>
          <span className="text-muted-foreground">{formatRelativeTime(tenant.createdAt, i18n.locale)}</span>
        </span>
      </SidePaneSection>
    </div>
  );
}

interface KpiRowProps {
  leftLabel: string;
  leftValue: React.ReactNode;
  leftLoading?: boolean;
  rightLabel: string;
  rightValue: React.ReactNode;
  rightLoading?: boolean;
}

function KpiRow({ leftLabel, leftValue, leftLoading, rightLabel, rightValue, rightLoading }: Readonly<KpiRowProps>) {
  return (
    <div className="grid grid-cols-2 gap-2">
      <div className="flex flex-col gap-1">
        <SubLabel>{leftLabel}</SubLabel>
        {leftLoading ? (
          <Skeleton className="h-5 w-24" />
        ) : (
          <span className="block h-5 text-sm leading-5 tabular-nums">{leftValue}</span>
        )}
      </div>
      <div className="flex flex-col gap-1 text-right">
        <SubLabel>{rightLabel}</SubLabel>
        {rightLoading ? (
          <Skeleton className="ml-auto h-5 w-24" />
        ) : (
          <span className="block h-5 text-sm leading-5 tabular-nums">{rightValue}</span>
        )}
      </div>
    </div>
  );
}

function SubLabel({ children }: Readonly<{ children: React.ReactNode }>) {
  return (
    <span className="block h-4 text-[0.6875rem] leading-4 font-semibold tracking-wider text-muted-foreground uppercase">
      {children}
    </span>
  );
}
