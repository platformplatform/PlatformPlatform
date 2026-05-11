import { useLingui } from "@lingui/react";
import { Trans } from "@lingui/react/macro";
import { Badge } from "@repo/ui/components/Badge";
import { Card } from "@repo/ui/components/Card";
import { Empty, EmptyDescription, EmptyHeader, EmptyTitle } from "@repo/ui/components/Empty";
import { Skeleton } from "@repo/ui/components/Skeleton";
import { TenantLogo } from "@repo/ui/components/TenantLogo";
import { useFormatDate } from "@repo/ui/hooks/useSmartDate";
import { getCountryFlagEmoji, getCountryName } from "@repo/ui/utils/countryFlag";
import { formatCurrency } from "@repo/utils/currency/formatCurrency";
import { Link } from "@tanstack/react-router";
import { CalendarIcon } from "lucide-react";

import type { components } from "@/shared/lib/api/client";

import { TenantStatusBadge } from "@/routes/accounts/-components/TenantStatusBadge";
import { PlannedSubscriptionChange } from "@/shared/lib/api/client";
import { getSubscriptionPlanLabel, getUserRoleLabel } from "@/shared/lib/api/labels";
import { getSubscriptionPlanBadgeClass } from "@/shared/lib/planBadge";

type BackOfficeUserDetailResponse = components["schemas"]["BackOfficeUserDetailResponse"];
type Membership = BackOfficeUserDetailResponse["tenantMemberships"][number];

interface UserTenantsSectionProps {
  user: BackOfficeUserDetailResponse | undefined;
}

export function UserTenantsSection({ user }: Readonly<UserTenantsSectionProps>) {
  return (
    <section className="flex flex-col gap-3">
      <div className="text-sm text-muted-foreground">
        <Trans>All accounts this user is a member of, with their plan and role.</Trans>
      </div>
      {!user ? (
        <Skeleton className="h-24 w-full" />
      ) : user.tenantMemberships.length === 0 ? (
        <Empty className="border bg-card">
          <EmptyHeader>
            <EmptyTitle>
              <Trans>No account memberships</Trans>
            </EmptyTitle>
            <EmptyDescription>
              <Trans>This user is not a member of any account.</Trans>
            </EmptyDescription>
          </EmptyHeader>
        </Empty>
      ) : (
        <div className="flex flex-col gap-2">
          {user.tenantMemberships.map((membership) => (
            <MembershipCard key={membership.tenantId} membership={membership} />
          ))}
        </div>
      )}
    </section>
  );
}

function MembershipCard({ membership }: Readonly<{ membership: Membership }>) {
  const { i18n } = useLingui();
  const formatDate = useFormatDate();
  const isCanceling = membership.plannedChange === PlannedSubscriptionChange.Cancellation;
  const isDowngrading = membership.plannedChange === PlannedSubscriptionChange.ScheduledPlanChange;
  const currentMrr =
    membership.monthlyRecurringRevenue !== null && membership.currency !== null
      ? formatCurrency(membership.monthlyRecurringRevenue, membership.currency)
      : null;
  const newMrr =
    isCanceling && membership.currency !== null
      ? formatCurrency(0, membership.currency)
      : isDowngrading && membership.scheduledPriceAmount !== null && membership.currency !== null
        ? formatCurrency(membership.scheduledPriceAmount, membership.currency)
        : null;
  return (
    <Card className="@container h-full overflow-hidden p-0 shadow-none">
      <Link
        to="/accounts/$tenantId"
        params={{ tenantId: membership.tenantId }}
        className="flex h-full flex-col gap-3 p-5 hover:bg-accent active:bg-accent @2xl:flex-row @2xl:items-center @2xl:gap-4"
      >
        <div className="flex items-center gap-4 @2xl:flex-1">
          <TenantLogo
            logoUrl={membership.tenantLogoUrl}
            tenantName={membership.tenantName}
            size="md"
            className="size-12"
          />
          <div className="flex min-w-0 flex-1 flex-col gap-2">
            <div className="flex min-w-0 items-center gap-2">
              <span className="truncate text-lg font-semibold">{membership.tenantName}</span>
              {membership.country && (
                <span className="flex shrink-0 items-center gap-1 text-xs text-muted-foreground">
                  <span aria-hidden="true">{getCountryFlagEmoji(membership.country)}</span>
                  <span className="hidden @[24rem]:inline">{getCountryName(membership.country, i18n.locale)}</span>
                </span>
              )}
            </div>
            <div className="flex flex-wrap items-center gap-2">
              <Badge variant="outline">{getUserRoleLabel(membership.role)}</Badge>
              <TenantStatusBadge
                plan={membership.plan}
                plannedChange={membership.plannedChange}
                hasEverSubscribed={membership.hasEverSubscribed}
              />
              <Badge className={`hidden w-fit @2xl:inline-flex ${getSubscriptionPlanBadgeClass(membership.plan)}`}>
                {getSubscriptionPlanLabel(membership.plan)}
              </Badge>
              {!membership.emailConfirmed && (
                <Badge variant="outline" className="border-amber-500/30 text-amber-700 dark:text-amber-300">
                  <Trans>Email pending</Trans>
                </Badge>
              )}
            </div>
          </div>
        </div>
        {/* Narrow (mobile) layout: stacked with divider, plan + renews on left, prices on right */}
        <div className="flex items-start justify-between gap-3 border-t pt-3 @2xl:hidden">
          <div className="flex flex-col items-start gap-1">
            <Badge className={`w-fit ${getSubscriptionPlanBadgeClass(membership.plan)}`}>
              {getSubscriptionPlanLabel(membership.plan)}
            </Badge>
            {membership.renewalDate && (
              <span className="inline-flex items-center gap-1.5 text-xs text-muted-foreground">
                <CalendarIcon className="size-3" aria-hidden={true} />
                <Trans>Renews {formatDate(membership.renewalDate)}</Trans>
              </span>
            )}
          </div>
          <div className="flex flex-col items-end gap-0.5 leading-tight tabular-nums">
            <div className="flex items-baseline gap-2">
              {newMrr && <span className="text-sm text-muted-foreground line-through">{currentMrr}</span>}
              {currentMrr && <span className="text-base font-semibold">{newMrr ?? currentMrr}</span>}
            </div>
            {currentMrr && (
              <span className="text-xs text-muted-foreground">
                <Trans>/ month</Trans>
              </span>
            )}
          </div>
        </div>
        {/* Wide layout: prices + /month inline, renews below, all right-aligned next to the badges */}
        <div className="hidden shrink-0 flex-col items-end gap-0.5 leading-tight @2xl:flex">
          {currentMrr && (
            <div className="flex items-baseline gap-2 tabular-nums">
              {newMrr && <span className="text-sm text-muted-foreground line-through">{currentMrr}</span>}
              <span className="text-base font-semibold">{newMrr ?? currentMrr}</span>
              <span className="text-xs text-muted-foreground">
                <Trans>/ month</Trans>
              </span>
            </div>
          )}
          {membership.renewalDate && (
            <span className="inline-flex items-center gap-1.5 text-xs text-muted-foreground">
              <CalendarIcon className="size-3" aria-hidden={true} />
              <Trans>Renews {formatDate(membership.renewalDate)}</Trans>
            </span>
          )}
        </div>
      </Link>
    </Card>
  );
}
