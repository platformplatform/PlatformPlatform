import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { LinkCard } from "@repo/ui/components/LinkCard";
import { Skeleton } from "@repo/ui/components/Skeleton";
import { useFormatDate } from "@repo/ui/hooks/useSmartDate";
import { formatCurrency } from "@repo/utils/currency/formatCurrency";
import { CalendarIcon } from "lucide-react";

import type { components } from "@/shared/lib/api/client";

import { api, SupportTicketStatus } from "@/shared/lib/api/client";

type AccountDetailTab = "users" | "invoices" | "billing-events" | "support-tickets";

type TenantDetailResponse = components["schemas"]["TenantDetailResponse"];

const isSubscriptionEnabled = import.meta.runtime_env.PUBLIC_SUBSCRIPTION_ENABLED === "true";
const isSupportSystemEnabled = import.meta.runtime_env.PUBLIC_SUPPORT_SYSTEM_ENABLED === "true";

interface AccountHealthTilesProps {
  tenant: TenantDetailResponse | undefined;
  tenantId: string;
  isLoading: boolean;
}

function formatAmount(amount: number | null, currency: string | null): string {
  if (amount === null || currency === null) {
    return "-";
  }
  return formatCurrency(amount, currency);
}

export function AccountHealthTiles({ tenant, tenantId, isLoading }: Readonly<AccountHealthTilesProps>) {
  const formatDate = useFormatDate();
  const userCountsQuery = api.useQuery("get", "/api/back-office/tenants/{id}/user-counts", {
    params: { path: { id: tenantId } }
  });
  const userCounts = userCountsQuery.data;

  // PageSize is requested at the validator-enforced cap of 100 so the chip count and the tab list
  // share one TanStack Query cache entry. For tenants with more than 100 tickets the chip undercounts
  // until a dedicated count endpoint exists; the v1 surface intentionally accepts that trade-off.
  // Skip the request entirely when the support system is gated off; the tile is hidden anyway.
  const supportTicketsQuery = api.useQuery(
    "get",
    "/api/back-office/support-tickets",
    { params: { query: { TenantId: tenantId, PageSize: 100 } } },
    { enabled: isSupportSystemEnabled }
  );
  const supportTicketsLoading = supportTicketsQuery.isLoading;
  const openTicketCount = supportTicketsQuery.data?.tickets.filter(
    (ticket) => ticket.status !== SupportTicketStatus.Resolved && ticket.status !== SupportTicketStatus.Closed
  ).length;
  const totalTicketCount = supportTicketsQuery.data?.totalCount;
  const totalUsers = userCounts?.totalUsers ?? 0;
  const activeUsers = userCounts?.activeUsers ?? 0;
  const pendingUsers = userCounts?.pendingUsers ?? 0;
  const inactiveUsers = Math.max(0, totalUsers - activeUsers - pendingUsers);
  const activePercent = totalUsers === 0 ? 0 : (activeUsers / totalUsers) * 100;
  const inactivePercent = totalUsers === 0 ? 0 : (inactiveUsers / totalUsers) * 100;
  const pendingPercent = totalUsers === 0 ? 0 : (pendingUsers / totalUsers) * 100;

  return (
    <div className="grid grid-cols-[repeat(auto-fit,minmax(13rem,1fr))] gap-4">
      <HealthTile label={t`Users`} loading={isLoading || userCountsQuery.isLoading} tenantId={tenantId} tab="users">
        {userCounts ? (
          <div className="flex flex-col gap-2">
            <span className="text-2xl font-semibold tabular-nums">{totalUsers}</span>
            <div className="flex h-1.5 w-full gap-0.5 overflow-hidden rounded-full bg-muted">
              {activePercent > 0 && (
                <div className="h-full rounded-full bg-success" style={{ width: `${activePercent}%` }} />
              )}
              {inactivePercent > 0 && (
                <div className="h-full rounded-full bg-warning" style={{ width: `${inactivePercent}%` }} />
              )}
              {pendingPercent > 0 && (
                <div className="h-full rounded-full bg-muted-foreground/40" style={{ width: `${pendingPercent}%` }} />
              )}
            </div>
            <span className="text-sm text-muted-foreground tabular-nums">
              <Trans>{activeUsers} active</Trans>
              {" · "}
              <Trans>{inactiveUsers} inactive</Trans>
              {" · "}
              <Trans>{pendingUsers} pending</Trans>
            </span>
          </div>
        ) : (
          <span className="text-2xl font-semibold tabular-nums">-</span>
        )}
      </HealthTile>

      {isSubscriptionEnabled && (
        <HealthTile
          label={t`Lifetime value`}
          loading={isLoading}
          tenantId={tenantId}
          tab="invoices"
          subtitle={
            tenant?.subscribedSince ? (
              <span className="inline-flex items-center gap-1.5">
                <CalendarIcon className="size-3" aria-hidden={true} />
                <Trans>Since {formatDate(tenant.subscribedSince)}</Trans>
              </span>
            ) : undefined
          }
        >
          <span className="text-2xl font-semibold tabular-nums">
            {tenant ? formatAmount(tenant.lifetimeValue, tenant.currency) : "-"}
          </span>
        </HealthTile>
      )}

      {isSubscriptionEnabled && (
        <HealthTile
          label={t`MRR`}
          loading={isLoading}
          tenantId={tenantId}
          tab="billing-events"
          subtitle={
            tenant?.renewalDate ? (
              <span className="inline-flex items-center gap-1.5">
                <CalendarIcon className="size-3" aria-hidden={true} />
                <Trans>Renews {formatDate(tenant.renewalDate)}</Trans>
              </span>
            ) : undefined
          }
        >
          <MrrAmount tenant={tenant} />
        </HealthTile>
      )}

      {isSupportSystemEnabled && (
        <HealthTile
          label={t`Support tickets`}
          loading={supportTicketsLoading}
          tenantId={tenantId}
          tab="support-tickets"
          subtitle={totalTicketCount !== undefined ? <Trans>{totalTicketCount} total</Trans> : undefined}
        >
          <span className="text-2xl font-semibold tabular-nums">
            {openTicketCount !== undefined ? openTicketCount : "-"}
          </span>
        </HealthTile>
      )}
    </div>
  );
}

function MrrAmount({ tenant }: Readonly<{ tenant: TenantDetailResponse | undefined }>) {
  if (!tenant) {
    return <span className="text-2xl font-semibold tabular-nums">-</span>;
  }

  const currentAmount = formatAmount(tenant.monthlyRecurringRevenue, tenant.currency);
  const isCanceling = tenant.cancelAtPeriodEnd;
  const isDowngrading = !isCanceling && tenant.scheduledPlan !== null;
  const newAmount =
    isCanceling && tenant.currency !== null
      ? formatAmount(0, tenant.currency)
      : isDowngrading && tenant.scheduledPriceAmount !== null
        ? formatAmount(tenant.scheduledPriceAmount, tenant.currency)
        : null;

  if (newAmount === null) {
    return <span className="text-2xl font-semibold tabular-nums">{currentAmount}</span>;
  }

  return (
    <div className="flex flex-col leading-tight">
      <span className="text-sm text-muted-foreground tabular-nums line-through">{currentAmount}</span>
      <span className="text-2xl font-semibold tabular-nums">{newAmount}</span>
    </div>
  );
}

function HealthTile({
  label,
  loading,
  subtitle,
  tenantId,
  tab,
  children
}: Readonly<{
  label: string;
  loading: boolean;
  subtitle?: React.ReactNode;
  tenantId: string;
  tab: AccountDetailTab;
  children: React.ReactNode;
}>) {
  return (
    <LinkCard
      to="/accounts/$tenantId"
      params={{ tenantId }}
      search={{ tab }}
      className="gap-2 rounded-lg p-4 shadow-none"
    >
      <span className="text-xs font-semibold tracking-wider text-muted-foreground uppercase">{label}</span>
      {loading ? (
        <>
          <Skeleton className="h-7 w-24" />
          <Skeleton className="h-4 w-20" />
        </>
      ) : (
        <>
          {children}
          {subtitle && <span className="text-xs text-muted-foreground">{subtitle}</span>}
        </>
      )}
    </LinkCard>
  );
}
