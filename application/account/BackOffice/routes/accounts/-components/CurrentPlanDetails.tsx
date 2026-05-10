import { Trans } from "@lingui/react/macro";
import { Badge } from "@repo/ui/components/Badge";
import { Card } from "@repo/ui/components/Card";
import { useFormatDate } from "@repo/ui/hooks/useSmartDate";
import { getCountryFlagEmoji } from "@repo/ui/utils/countryFlag";
import { formatCurrency } from "@repo/utils/currency/formatCurrency";
import { CalendarClockIcon, CalendarIcon, XCircleIcon } from "lucide-react";

import type { components } from "@/shared/lib/api/client";

import { getSubscriptionPlanLabel } from "@/shared/lib/api/labels";
import { getSubscriptionPlanBadgeClass } from "@/shared/lib/planBadge";

import { CardBrandLogo } from "./CardBrandLogo";

type TenantDetailResponse = components["schemas"]["TenantDetailResponse"];

export function CurrentPlanDetails({ tenant }: Readonly<{ tenant: TenantDetailResponse }>) {
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
        tenant.billingAddress.state
      ].filter((value): value is string => Boolean(value && value.trim().length > 0))
    : [];

  const country = tenant.billingAddress?.country?.trim() ?? "";
  const hasCustomer = Boolean(tenant.billingName || tenant.taxId);

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
              {isCanceling ? <Trans>Expires</Trans> : <Trans>Renewal date</Trans>}
            </dt>
            <dd className="inline-flex items-center gap-1.5 tabular-nums">
              {tenant.renewalDate && <CalendarIcon className="size-3.5 text-muted-foreground" aria-hidden={true} />}
              {tenant.renewalDate ? formatDate(tenant.renewalDate) : "-"}
            </dd>
          </div>
        </dl>

        <hr className="border-border md:hidden lg:block" />

        {hasCustomer && (
          <div className="flex flex-col gap-2 md:flex-1">
            <span className="text-[0.6875rem] font-semibold tracking-wider text-muted-foreground uppercase">
              <Trans>Customer</Trans>
            </span>
            {tenant.billingName && <div className="text-sm">{tenant.billingName}</div>}
            {tenant.taxId && (
              <div className="text-sm text-muted-foreground tabular-nums">
                <Trans>VAT</Trans> {tenant.taxId}
              </div>
            )}
          </div>
        )}

        {hasCustomer && <hr className="border-border md:hidden lg:block" />}

        <div className="flex flex-col gap-2 md:flex-1">
          <span className="text-[0.6875rem] font-semibold tracking-wider text-muted-foreground uppercase">
            <Trans>Billing address</Trans>
          </span>
          {billingAddressLines.length === 0 && country === "" ? (
            <span className="text-sm text-muted-foreground">
              <Trans>No billing address on file.</Trans>
            </span>
          ) : (
            <address className="text-sm leading-6 not-italic">
              {billingAddressLines.map((line) => (
                <div key={line}>{line}</div>
              ))}
              {country !== "" && (
                <div className="inline-flex items-center gap-1.5">
                  <span aria-hidden={true}>{getCountryFlagEmoji(country)}</span>
                  <span>{country}</span>
                </div>
              )}
            </address>
          )}
        </div>

        {tenant.paymentMethod && (
          <>
            <hr className="border-border md:hidden lg:block" />
            <div className="flex flex-col gap-2 md:flex-1">
              <span className="text-[0.6875rem] font-semibold tracking-wider text-muted-foreground uppercase">
                <Trans>Payment method</Trans>
              </span>
              <div className="inline-flex items-center gap-2 text-sm">
                <CardBrandLogo brand={tenant.paymentMethod.brand} />
                <span className="tabular-nums">•••• {tenant.paymentMethod.last4}</span>
                <span className="text-muted-foreground tabular-nums">
                  {tenant.paymentMethod.expMonth.toString().padStart(2, "0")}/
                  {tenant.paymentMethod.expYear.toString().slice(-2)}
                </span>
              </div>
            </div>
          </>
        )}
      </div>
    </Card>
  );
}
