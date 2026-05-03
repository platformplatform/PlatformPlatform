import { Trans } from "@lingui/react/macro";
import { Badge } from "@repo/ui/components/Badge";
import { Card } from "@repo/ui/components/Card";
import { Skeleton } from "@repo/ui/components/Skeleton";
import { useFormatDate } from "@repo/ui/hooks/useSmartDate";
import { formatCurrency } from "@repo/utils/currency/formatCurrency";
import { CalendarClockIcon, XCircleIcon } from "lucide-react";

import type { components } from "@/shared/lib/api/client";

import { getSubscriptionPlanLabel } from "@/shared/lib/api/labels";
import { getSubscriptionPlanBadgeClass } from "@/shared/lib/planBadge";

type TenantDetailResponse = components["schemas"]["TenantDetailResponse"];

interface AccountCurrentPlanCardProps {
  tenant: TenantDetailResponse | undefined;
  isLoading: boolean;
}

export function AccountCurrentPlanCard({ tenant, isLoading }: Readonly<AccountCurrentPlanCardProps>) {
  const formatDate = useFormatDate();

  if (isLoading || !tenant) {
    return (
      <section className="flex flex-col">
        <h4 className="mb-3">
          <Trans>Current plan</Trans>
        </h4>
        <Card className="gap-4 rounded-lg p-5 py-5 shadow-none">
          <Skeleton className="h-5 w-24" />
          <Skeleton className="h-9 w-40" />
          <Skeleton className="h-4 w-32" />
          <Skeleton className="h-px w-full" />
          <Skeleton className="h-4 w-full" />
          <Skeleton className="h-4 w-full" />
        </Card>
      </section>
    );
  }

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
    <section className="flex flex-col">
      <h4 className="mb-3">
        <Trans>Current plan</Trans>
      </h4>
      <Card className="gap-4 rounded-lg p-5 py-5 shadow-none">
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
              <dd className="tabular-nums">{tenant.subscribedSince ? formatDate(tenant.subscribedSince) : "-"}</dd>
            </div>
            <div className="flex flex-col gap-1">
              <dt className="text-[0.6875rem] font-semibold tracking-wider text-muted-foreground uppercase">
                <Trans>Renewal date</Trans>
              </dt>
              <dd className="tabular-nums">{tenant.renewalDate ? formatDate(tenant.renewalDate) : "-"}</dd>
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
    </section>
  );
}
