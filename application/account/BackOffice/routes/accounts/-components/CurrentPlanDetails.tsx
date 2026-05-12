import { Trans } from "@lingui/react/macro";
import { Badge } from "@repo/ui/components/Badge";
import { Card } from "@repo/ui/components/Card";
import { useFormatDate } from "@repo/ui/hooks/useSmartDate";
import { getCountryFlagEmoji } from "@repo/ui/utils/countryFlag";
import { formatCurrency } from "@repo/utils/currency/formatCurrency";
import { CalendarClockIcon, XCircleIcon } from "lucide-react";

import type { components } from "@/shared/lib/api/client";

import { getSubscriptionPlanLabel } from "@/shared/lib/api/labels";
import { getSubscriptionPlanBadgeClass } from "@/shared/lib/planBadge";

import { CardBrandLogo } from "./CardBrandLogo";

type TenantDetailResponse = components["schemas"]["TenantDetailResponse"];
type PaymentMethodResponse = NonNullable<TenantDetailResponse["paymentMethod"]>;

const sectionLabelClassName = "text-[0.6875rem] font-semibold tracking-wider text-muted-foreground uppercase";

export function CurrentPlanDetails({ tenant }: Readonly<{ tenant: TenantDetailResponse }>) {
  const formatDate = useFormatDate();

  const monthlyAmount =
    tenant.monthlyRecurringRevenue !== null && tenant.currency !== null
      ? formatCurrency(tenant.monthlyRecurringRevenue, tenant.currency)
      : "-";

  const isCanceling = tenant.cancelAtPeriodEnd;
  const isDowngrading = !isCanceling && tenant.scheduledPlan !== null;
  const isCanceled = tenant.plan === "Basis" && tenant.hasEverSubscribed && !isCanceling && !isDowngrading;
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
  const hasAnyBillingDetails =
    Boolean(tenant.billingName) || billingAddressLines.length > 0 || country !== "" || Boolean(tenant.taxId);

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

      <div className="grid grid-cols-2 gap-x-6 gap-y-4 text-sm">
        <div className="flex flex-col gap-1">
          <span className={sectionLabelClassName}>
            <Trans>Subscribed since</Trans>
          </span>
          <span className="tabular-nums">{tenant.subscribedSince ? formatDate(tenant.subscribedSince) : "-"}</span>
        </div>

        <div className="flex flex-col gap-1">
          <span className={sectionLabelClassName}>
            {isCanceled ? <Trans>Expired</Trans> : isCanceling ? <Trans>Expires</Trans> : <Trans>Renewal date</Trans>}
          </span>
          <span className="whitespace-nowrap tabular-nums">
            {tenant.renewalDate ? formatDate(tenant.renewalDate) : "-"}
          </span>
        </div>

        <hr className="col-span-2 border-border" />

        <div className="flex flex-col gap-2">
          <span className={sectionLabelClassName}>
            <Trans>Billing address</Trans>
          </span>
          {hasAnyBillingDetails ? (
            <address className="text-sm leading-6 not-italic">
              {tenant.billingName && <div>{tenant.billingName}</div>}
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
          ) : (
            <span className="text-muted-foreground">
              <Trans>No billing address on file.</Trans>
            </span>
          )}
        </div>

        <div className="flex flex-col gap-4">
          <div className="flex flex-col gap-2">
            <span className={sectionLabelClassName}>
              <Trans>Payment method</Trans>
            </span>
            {tenant.paymentMethod ? (
              <PaymentMethodBlock paymentMethod={tenant.paymentMethod} />
            ) : (
              <span className="text-muted-foreground">-</span>
            )}
          </div>

          {tenant.taxId && (
            <div className="flex flex-col gap-2">
              <span className={sectionLabelClassName}>
                <Trans>VAT number</Trans>
              </span>
              <span className="tabular-nums">{tenant.taxId}</span>
            </div>
          )}
        </div>
      </div>
    </Card>
  );
}

// Renders the brand panel for every payment method. Stripe Link is funded by an underlying card that
// the pinned Stripe.NET SDK does not expose, so the backend emits a ("link", "****", 0, 0) sentinel;
// rendering the bullet line and expiry for "link" would surface placeholder data, so we suppress them.
function PaymentMethodBlock({ paymentMethod }: Readonly<{ paymentMethod: PaymentMethodResponse }>) {
  const isLink = paymentMethod.brand.toLowerCase() === "link";
  const hasExpiry = paymentMethod.expMonth > 0 && paymentMethod.expYear > 0;

  return (
    <div className="flex items-center gap-2">
      <CardBrandLogo brand={paymentMethod.brand} size="lg" />
      {!isLink && (
        <div className="flex flex-col leading-tight">
          <span className="whitespace-nowrap tabular-nums">•••• {paymentMethod.last4}</span>
          {hasExpiry && (
            <span className="whitespace-nowrap text-muted-foreground tabular-nums">
              {paymentMethod.expMonth.toString().padStart(2, "0")}/{paymentMethod.expYear.toString().slice(-2)}
            </span>
          )}
        </div>
      )}
    </div>
  );
}
