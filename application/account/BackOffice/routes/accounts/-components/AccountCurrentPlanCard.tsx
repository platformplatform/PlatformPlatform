import type { ReactNode } from "react";

import { Trans } from "@lingui/react/macro";
import { Badge } from "@repo/ui/components/Badge";
import { Card } from "@repo/ui/components/Card";
import { Empty, EmptyDescription, EmptyHeader, EmptyTitle } from "@repo/ui/components/Empty";
import { Skeleton } from "@repo/ui/components/Skeleton";
import { useFormatDate } from "@repo/ui/hooks/useSmartDate";
import { formatCurrency } from "@repo/utils/currency/formatCurrency";
import { CalendarClockIcon, CalendarIcon, XCircleIcon } from "lucide-react";

import type { components } from "@/shared/lib/api/client";

import { getSubscriptionPlanLabel } from "@/shared/lib/api/labels";
import { getSubscriptionPlanBadgeClass } from "@/shared/lib/planBadge";

type TenantDetailResponse = components["schemas"]["TenantDetailResponse"];

interface AccountCurrentPlanCardProps {
  tenant: TenantDetailResponse | undefined;
  isLoading: boolean;
}

export function AccountCurrentPlanCard({ tenant, isLoading }: Readonly<AccountCurrentPlanCardProps>) {
  if (isLoading || !tenant) {
    return <CurrentPlanShell>{renderSkeleton()}</CurrentPlanShell>;
  }

  const isFree = tenant.subscribedSince === null && !tenant.hasEverSubscribed;
  if (isFree) {
    return (
      <CurrentPlanShell>
        <CurrentPlanEmpty title={<Trans>No plan</Trans>} description={<Trans>No paid plan yet.</Trans>} />
      </CurrentPlanShell>
    );
  }

  const isCanceled = tenant.subscribedSince === null && tenant.hasEverSubscribed;
  if (isCanceled) {
    return (
      <CurrentPlanShell>
        <CurrentPlanEmpty
          title={<Trans>Subscription canceled</Trans>}
          description={<Trans>This account previously had a paid subscription that ended.</Trans>}
        />
      </CurrentPlanShell>
    );
  }

  return (
    <CurrentPlanShell>
      <CurrentPlanDetails tenant={tenant} />
    </CurrentPlanShell>
  );
}

function CurrentPlanShell({ children }: Readonly<{ children: ReactNode }>) {
  return (
    <section className="flex h-full flex-col">
      <h4 className="mb-3 whitespace-nowrap">
        <Trans>Current plan</Trans>
      </h4>
      {children}
    </section>
  );
}

function CurrentPlanEmpty({ title, description }: Readonly<{ title: ReactNode; description: ReactNode }>) {
  return (
    <Empty className="h-[8.375rem] flex-none border bg-card p-4 md:p-4 lg:h-auto lg:min-h-[20.75rem] lg:flex-1 lg:p-12">
      <EmptyHeader>
        <EmptyTitle>{title}</EmptyTitle>
        <EmptyDescription>{description}</EmptyDescription>
      </EmptyHeader>
    </Empty>
  );
}

function renderSkeleton() {
  return (
    <Card className="flex-1 gap-4 rounded-lg p-5 py-5 shadow-none">
      <Skeleton className="h-5 w-24" />
      <Skeleton className="h-9 w-40" />
      <Skeleton className="h-4 w-32" />
      <Skeleton className="h-px w-full" />
      <Skeleton className="h-4 w-full" />
      <Skeleton className="h-4 w-full" />
    </Card>
  );
}

function CurrentPlanDetails({ tenant }: Readonly<{ tenant: TenantDetailResponse }>) {
  const formatDate = useFormatDate();

  const monthlyAmount =
    tenant.monthlyRecurringRevenue !== null && tenant.currency !== null
      ? formatCurrency(tenant.monthlyRecurringRevenue, tenant.currency)
      : "-";

  const isCanceling = tenant.cancelAtPeriodEnd;
  const isDowngrading = !isCanceling && tenant.scheduledPlan !== null;
  const newMonthlyAmount =
    isCanceling && tenant.currency !== null
      ? formatCurrency(0, tenant.currency)
      : isDowngrading && tenant.scheduledPriceAmount !== null && tenant.currency !== null
        ? formatCurrency(tenant.scheduledPriceAmount, tenant.currency)
        : null;
  const showStrikedAmount = (isCanceling || isDowngrading) && newMonthlyAmount !== null;

  const billingAddressLines = tenant.billingAddress
    ? [
        tenant.billingAddress.line1,
        tenant.billingAddress.line2,
        [tenant.billingAddress.postalCode, tenant.billingAddress.city].filter(Boolean).join(" ").trim() || null,
        tenant.billingAddress.state,
        tenant.billingAddress.country
      ].filter((value): value is string => Boolean(value && value.trim().length > 0))
    : [];

  return (
    <Card className="min-h-[8.375rem] flex-1 gap-4 rounded-lg p-5 py-5 shadow-none lg:min-h-[20.75rem]">
      <div className="flex min-w-0 flex-col gap-1">
        <div className="flex min-w-0 items-start justify-between gap-2">
          {showStrikedAmount ? (
            <div className="flex min-w-0 flex-col leading-tight">
              <span className="text-base text-muted-foreground tabular-nums line-through">{monthlyAmount}</span>
              <span className="text-3xl font-semibold tabular-nums">{newMonthlyAmount}</span>
            </div>
          ) : (
            <span className="min-w-0 truncate text-3xl font-semibold tabular-nums">{monthlyAmount}</span>
          )}
          <div className="flex shrink-0 flex-col items-end gap-1.5">
            <Badge className={getSubscriptionPlanBadgeClass(tenant.plan)}>
              {getSubscriptionPlanLabel(tenant.plan)}
            </Badge>
            {isCanceling ? (
              <Badge variant="destructive" className="gap-1">
                <XCircleIcon className="size-3" />
                <Trans>Canceling</Trans>
              </Badge>
            ) : isDowngrading ? (
              <Badge variant="warning" className="gap-1">
                <CalendarClockIcon className="size-3" />
                <Trans>Downgrading</Trans>
              </Badge>
            ) : null}
          </div>
        </div>
        <span className="text-sm text-muted-foreground">
          <Trans>per month, billed monthly</Trans>
        </span>
      </div>

      <hr className="border-border" />

      <div className="flex flex-col gap-4 md:flex-row-reverse md:gap-8 lg:flex-col lg:gap-4">
        <dl className="grid grid-cols-2 gap-3 text-sm md:flex md:flex-1 md:flex-col md:gap-3 lg:grid lg:grid-cols-2">
          <div className="flex flex-col gap-1">
            <dt className="text-[0.6875rem] font-semibold tracking-wider text-muted-foreground uppercase">
              <Trans>Subscribed since</Trans>
            </dt>
            <dd className="inline-flex items-center gap-1.5 tabular-nums">
              {tenant.subscribedSince && <CalendarIcon className="size-3.5 text-muted-foreground" aria-hidden={true} />}
              {tenant.subscribedSince ? formatDate(tenant.subscribedSince) : "-"}
            </dd>
          </div>
          <div className="flex flex-col gap-1">
            <dt className="text-[0.6875rem] font-semibold tracking-wider text-muted-foreground uppercase">
              <Trans>Renewal date</Trans>
            </dt>
            <dd className="inline-flex items-center gap-1.5 tabular-nums">
              {tenant.renewalDate && <CalendarIcon className="size-3.5 text-muted-foreground" aria-hidden={true} />}
              {tenant.renewalDate ? formatDate(tenant.renewalDate) : "-"}
            </dd>
          </div>
        </dl>

        <hr className="border-border md:hidden lg:block" />

        <div className="flex flex-col gap-2 md:flex-1">
          <span className="text-[0.6875rem] font-semibold tracking-wider text-muted-foreground uppercase">
            <Trans>Billing address</Trans>
          </span>
          {billingAddressLines.length === 0 ? (
            <span className="text-sm text-muted-foreground">
              <Trans>No billing address on file.</Trans>
            </span>
          ) : (
            <address className="text-sm leading-6 not-italic">
              {billingAddressLines.map((line) => (
                <div key={line}>{line}</div>
              ))}
            </address>
          )}
        </div>
      </div>
    </Card>
  );
}
