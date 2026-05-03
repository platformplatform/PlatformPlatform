import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { Badge } from "@repo/ui/components/Badge";
import { Skeleton } from "@repo/ui/components/Skeleton";
import { useFormatDate } from "@repo/ui/hooks/useSmartDate";
import { formatCurrency } from "@repo/utils/currency/formatCurrency";

import type { components } from "@/shared/lib/api/client";

import { api, UserRole } from "@/shared/lib/api/client";
import { getSubscriptionPlanLabel } from "@/shared/lib/api/labels";
import { getSubscriptionPlanBadgeClass } from "@/shared/lib/planBadge";

import { SidePaneDivider, SidePaneSection } from "./SidePaneSection";
import { SidePaneUserList } from "./SidePaneUserList";
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

function formatAmount(amount: number | null, currency: string | null): string {
  if (amount === null || currency === null) {
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

  const userCountsQuery = api.useQuery(
    "get",
    "/api/back-office/tenants/{id}/user-counts",
    { params: { path: { id: debouncedTenantId } } },
    { enabled: detailReady }
  );

  const ownersQuery = api.useQuery(
    "get",
    "/api/back-office/tenants/{id}/users",
    { params: { path: { id: debouncedTenantId }, query: { Role: UserRole.Owner, PageSize: 100 } } },
    { enabled: detailReady }
  );

  const paymentHistoryQuery = api.useQuery(
    "get",
    "/api/back-office/tenants/{id}/payment-history",
    { params: { path: { id: debouncedTenantId }, query: { PageSize: 1 } } },
    { enabled: detailReady }
  );

  const lastInvoice = paymentHistoryQuery.data?.transactions[0] ?? null;
  const hasEverHadSubscription = (paymentHistoryQuery.data?.totalCount ?? 0) > 0;
  const paymentHistoryLoading = !detailReady || paymentHistoryQuery.isLoading;

  return (
    <div className="flex flex-col">
      <SidePaneDivider />

      <SidePaneSection label={t`Plan & revenue`}>
        <div className="flex flex-col gap-3">
          <div className="grid grid-cols-2 gap-2">
            <div className="flex flex-col gap-1">
              <SubLabel>
                <Trans>MRR</Trans>
              </SubLabel>
              <span className="text-sm tabular-nums">
                {formatAmount(tenant.monthlyRecurringRevenue, tenant.currency)}
              </span>
            </div>
            <div className="flex flex-col gap-1 text-right">
              <SubLabel>
                <Trans>Lifetime value</Trans>
              </SubLabel>
              {detailLoading ? (
                <Skeleton className="ml-auto h-4 w-24" />
              ) : (
                <span className="text-sm tabular-nums">
                  {detail ? formatAmount(detail.lifetimeValue, detail.currency) : "-"}
                </span>
              )}
            </div>
          </div>

          <div className="grid grid-cols-2 gap-2">
            <div className="flex flex-col gap-1">
              <SubLabel>
                <Trans>Plan</Trans>
              </SubLabel>
              <Badge className={`w-fit ${getSubscriptionPlanBadgeClass(tenant.plan)}`}>
                {getSubscriptionPlanLabel(tenant.plan)}
              </Badge>
            </div>
            <div className="flex flex-col gap-1 text-right">
              <SubLabel>
                <Trans>Renewal</Trans>
              </SubLabel>
              <span className="text-sm tabular-nums">{tenant.renewalDate ? formatDate(tenant.renewalDate) : "-"}</span>
            </div>
          </div>

          <SubscriptionStatusIndicator
            plannedChange={tenant.plannedChange}
            state={detail?.state}
            scheduledPlan={detail?.scheduledPlan ?? null}
          />

          {paymentHistoryLoading ? (
            <div className="grid grid-cols-2 gap-2">
              <div className="flex flex-col gap-1">
                <Skeleton className="h-3 w-12" />
                <Skeleton className="h-4 w-20" />
              </div>
              <div className="flex flex-col gap-1 text-right">
                <Skeleton className="ml-auto h-3 w-16" />
                <Skeleton className="ml-auto h-4 w-24" />
              </div>
            </div>
          ) : (
            hasEverHadSubscription && (
              <div className="grid grid-cols-2 gap-2">
                <div className="flex flex-col gap-1">
                  <SubLabel>
                    <Trans>Amount</Trans>
                  </SubLabel>
                  <span className="text-sm tabular-nums">
                    {lastInvoice ? formatCurrency(lastInvoice.amount, lastInvoice.currency) : "-"}
                  </span>
                </div>
                <div className="flex flex-col gap-1 text-right">
                  <SubLabel>
                    <Trans>Last invoice</Trans>
                  </SubLabel>
                  <span className="text-sm tabular-nums">{lastInvoice ? formatDate(lastInvoice.date) : "-"}</span>
                </div>
              </div>
            )
          )}
        </div>
      </SidePaneSection>

      <SidePaneDivider />

      <SidePaneSection label={t`Owners`}>
        <SidePaneUserList
          users={ownersQuery.data?.users ?? []}
          isLoading={!detailReady || ownersQuery.isLoading}
          emptyMessage={t`No owners on this account.`}
        />
      </SidePaneSection>

      <SidePaneDivider />

      <SidePaneSection label={t`Users`}>
        {!detailReady || userCountsQuery.isLoading ? (
          <Skeleton className="h-4 w-24" />
        ) : userCountsQuery.data ? (
          <span className="text-sm tabular-nums">
            <Trans>
              {userCountsQuery.data.activeUsers} active / {userCountsQuery.data.totalUsers} total
            </Trans>
          </span>
        ) : (
          <span className="text-sm text-muted-foreground">-</span>
        )}
      </SidePaneSection>
    </div>
  );
}

function SubLabel({ children }: Readonly<{ children: React.ReactNode }>) {
  return (
    <span className="text-[0.6875rem] font-semibold tracking-wider text-muted-foreground uppercase">{children}</span>
  );
}
