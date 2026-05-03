import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { Badge } from "@repo/ui/components/Badge";
import { Button } from "@repo/ui/components/Button";
import { SidePane, SidePaneBody, SidePaneFooter, SidePaneHeader } from "@repo/ui/components/SidePane";
import { Skeleton } from "@repo/ui/components/Skeleton";
import { useDebounce } from "@repo/ui/hooks/useDebounce";
import { useFormatDate } from "@repo/ui/hooks/useSmartDate";
import { formatCurrency } from "@repo/utils/currency/formatCurrency";
import { useNavigate } from "@tanstack/react-router";
import { ArrowRightIcon, CalendarClockIcon, XCircleIcon } from "lucide-react";

import type { components } from "@/shared/lib/api/client";

import { api, PlannedSubscriptionChange, UserRole } from "@/shared/lib/api/client";
import { getSubscriptionPlanLabel } from "@/shared/lib/api/labels";
import { getCountryFlagEmoji } from "@repo/ui/utils/countryFlag";

import { SidePaneUserList } from "./SidePaneUserList";

function formatMonthlyRevenue(amount: number | null, currency: string | null): string {
  if (amount === null || currency === null) {
    return "-";
  }
  return formatCurrency(amount, currency);
}

type TenantSummary = components["schemas"]["TenantSummary"];

interface AccountSidePaneProps {
  tenant: TenantSummary | null;
  isOpen: boolean;
  onClose: () => void;
}

const USER_DATA_DEBOUNCE_MS = 2000;

export function AccountSidePane({ tenant, isOpen, onClose }: Readonly<AccountSidePaneProps>) {
  const navigate = useNavigate();
  const formatDate = useFormatDate();

  const tenantId = tenant?.id;
  const debouncedTenantId = useDebounce(tenantId, USER_DATA_DEBOUNCE_MS);
  const userDataReady = Boolean(debouncedTenantId) && debouncedTenantId === tenantId;

  const userCountsQuery = api.useQuery(
    "get",
    "/api/back-office/tenants/{id}/user-counts",
    { params: { path: { id: debouncedTenantId ?? "" } } },
    { enabled: userDataReady }
  );

  const ownersQuery = api.useQuery(
    "get",
    "/api/back-office/tenants/{id}/users",
    { params: { path: { id: debouncedTenantId ?? "" }, query: { Role: UserRole.Owner, PageSize: 100 } } },
    { enabled: userDataReady }
  );

  const handleOpen = () => {
    if (!tenant) {
      return;
    }
    navigate({ to: "/accounts/$tenantId", params: { tenantId: tenant.id } });
  };

  return (
    <SidePane
      isOpen={isOpen}
      onOpenChange={(open) => !open && onClose()}
      trackingTitle="Account preview"
      trackingKey={tenant?.id}
      aria-label={t`Account preview`}
    >
      <SidePaneHeader closeButtonLabel={t`Close account preview`}>
        <span className="truncate">{tenant?.name ?? <Trans>Account</Trans>}</span>
      </SidePaneHeader>

      <SidePaneBody>
        {tenant && (
          <div className="flex flex-col gap-6">
            <Section label={t`Plan & revenue`}>
              <div className="flex flex-col gap-2">
                <div className="flex items-center justify-between gap-3">
                  <Badge variant="outline">{getSubscriptionPlanLabel(tenant.plan)}</Badge>
                  <span className="text-sm font-medium tabular-nums">
                    {formatMonthlyRevenue(tenant.monthlyRecurringRevenue, tenant.currency)}
                  </span>
                </div>
                {tenant.renewalDate && (
                  <div className="flex items-center justify-between gap-3 text-sm">
                    <span className="text-muted-foreground">
                      <Trans>Renewal</Trans>
                    </span>
                    <span>{formatDate(tenant.renewalDate)}</span>
                  </div>
                )}
                {tenant.plannedChange === PlannedSubscriptionChange.Cancellation && (
                  <Badge variant="outline" className="w-fit gap-1 border-destructive/30 text-destructive">
                    <XCircleIcon className="size-3" />
                    <Trans>Cancellation at period end</Trans>
                  </Badge>
                )}
                {tenant.plannedChange === PlannedSubscriptionChange.ScheduledPlanChange && (
                  <Badge variant="outline" className="w-fit gap-1">
                    <CalendarClockIcon className="size-3" />
                    <Trans>Scheduled plan change</Trans>
                  </Badge>
                )}
              </div>
            </Section>

            <Section label={t`Country`}>
              {tenant.country ? (
                <span className="inline-flex items-center gap-2 text-sm">
                  <span aria-hidden={true}>{getCountryFlagEmoji(tenant.country)}</span>
                  <span>{tenant.country}</span>
                </span>
              ) : (
                <span className="text-sm text-muted-foreground">-</span>
              )}
            </Section>

            <Section label={t`Created`}>
              <span className="text-sm">{formatDate(tenant.createdAt)}</span>
            </Section>

            <Section label={t`Owners`}>
              <SidePaneUserList
                users={ownersQuery.data?.users ?? []}
                isLoading={!userDataReady || ownersQuery.isLoading}
                emptyMessage={t`No owners on this account.`}
              />
            </Section>

            <Section label={t`Users`}>
              {!userDataReady || userCountsQuery.isLoading ? (
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
            </Section>
          </div>
        )}
      </SidePaneBody>

      <SidePaneFooter>
        <Button onClick={handleOpen} className="w-full justify-center" disabled={!tenant}>
          <Trans>Open account</Trans>
          <ArrowRightIcon className="size-4" />
        </Button>
      </SidePaneFooter>
    </SidePane>
  );
}

function Section({ label, children }: Readonly<{ label: string; children: React.ReactNode }>) {
  return (
    <div className="flex flex-col gap-2">
      <span className="text-xs font-semibold tracking-wider text-muted-foreground uppercase">{label}</span>
      {children}
    </div>
  );
}
