import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { Badge } from "@repo/ui/components/Badge";
import { Button } from "@repo/ui/components/Button";
import { Separator } from "@repo/ui/components/Separator";
import { formatCurrency } from "@repo/utils/currency/formatCurrency";
import { CheckIcon } from "lucide-react";

import { SubscriptionPlan } from "@/shared/lib/api/client";

import { getPlanDetails, getPlanOrder } from "./planUtils";

export { getFormattedPrice, getCatalogUnitAmount } from "./planUtils";
export { getPlanDetails };

type PlanCardProps = {
  plan: SubscriptionPlan;
  formattedPrice: string;
  currentPlan: SubscriptionPlan;
  cancelAtPeriodEnd: boolean;
  scheduledPlan: SubscriptionPlan | null;
  isStripeConfigured: boolean;
  onSubscribe: (plan: SubscriptionPlan) => void;
  onUpgrade: (plan: SubscriptionPlan) => void;
  onDowngrade: (plan: SubscriptionPlan) => void;
  onReactivate: () => void;
  onCancelDowngrade: () => void;
  isPending: boolean;
  pendingPlan: SubscriptionPlan | null;
  isCancelDowngradePending: boolean;
  currentPriceAmount?: number | null;
  currentPriceCurrency?: string | null;
  catalogUnitAmount?: number | null;
  taxExclusive?: boolean;
};

export function PlanCard({
  plan,
  formattedPrice,
  currentPlan,
  cancelAtPeriodEnd,
  scheduledPlan,
  isStripeConfigured,
  onSubscribe,
  onUpgrade,
  onDowngrade,
  onReactivate,
  onCancelDowngrade,
  isPending,
  pendingPlan,
  isCancelDowngradePending,
  currentPriceAmount,
  currentPriceCurrency,
  catalogUnitAmount,
  taxExclusive
}: Readonly<PlanCardProps>) {
  const details = getPlanDetails(plan);
  const isCurrent = plan === currentPlan;
  const isScheduled = plan === scheduledPlan;
  const isThisPlanPending = pendingPlan === plan;
  const currentOrder = getPlanOrder(currentPlan);
  const planOrder = getPlanOrder(plan);
  const isUpgrade = planOrder > currentOrder;
  const isDowngrade = planOrder < currentOrder;
  const isBasis = currentPlan === SubscriptionPlan.Basis;

  function renderAction() {
    if (cancelAtPeriodEnd) {
      if (!isCurrent) {
        return null;
      }
      return (
        <Button variant="default" className="w-full" onClick={onReactivate} disabled={isPending || !isStripeConfigured}>
          {isThisPlanPending ? <Trans>Processing...</Trans> : <Trans>Reactivate</Trans>}
        </Button>
      );
    }

    if (isCurrent) {
      return (
        <Button variant="outline" className="w-full" disabled={true}>
          <Trans>Current plan</Trans>
        </Button>
      );
    }

    if (isBasis && plan !== SubscriptionPlan.Basis) {
      return (
        <Button
          variant="default"
          className="w-full"
          onClick={() => onSubscribe(plan)}
          disabled={isPending || !isStripeConfigured}
        >
          {isThisPlanPending ? <Trans>Processing...</Trans> : <Trans>Subscribe</Trans>}
        </Button>
      );
    }

    if (isUpgrade) {
      return (
        <Button
          variant="default"
          className="w-full"
          onClick={() => onUpgrade(plan)}
          disabled={isPending || !isStripeConfigured}
        >
          {isThisPlanPending ? <Trans>Processing...</Trans> : <Trans>Upgrade</Trans>}
        </Button>
      );
    }

    if (isDowngrade) {
      if (isScheduled) {
        return (
          <Button
            className="w-full"
            onClick={onCancelDowngrade}
            disabled={isPending || isCancelDowngradePending || !isStripeConfigured}
          >
            {isCancelDowngradePending ? <Trans>Processing...</Trans> : <Trans>Cancel downgrade</Trans>}
          </Button>
        );
      }
      return (
        <Button
          variant="secondary"
          className="w-full"
          onClick={() => onDowngrade(plan)}
          disabled={isPending || !isStripeConfigured}
        >
          {isThisPlanPending ? <Trans>Processing...</Trans> : <Trans>Downgrade</Trans>}
        </Button>
      );
    }

    return null;
  }

  return (
    <div
      className={`flex flex-col gap-4 rounded-lg border p-6 ${isCurrent ? "border-primary ring-1 ring-primary" : "border-border"}`}
    >
      <div className="flex items-center justify-between">
        <h3 className="m-0">{details.name}</h3>
        {isCurrent && (
          <Badge variant="default">
            <Trans>Current</Trans>
          </Badge>
        )}
      </div>
      <div className="flex flex-col gap-1">
        <div className="text-2xl font-semibold">
          {isCurrent &&
          currentPriceAmount != null &&
          currentPriceCurrency != null &&
          catalogUnitAmount != null &&
          currentPriceAmount !== catalogUnitAmount ? (
            <div className="flex flex-wrap items-baseline gap-2">
              <span>{t`${formatCurrency(currentPriceAmount, currentPriceCurrency)}/month`}</span>
              <span className="text-base text-muted-foreground line-through">{formattedPrice}</span>
            </div>
          ) : (
            formattedPrice
          )}
        </div>
        {taxExclusive && plan !== SubscriptionPlan.Basis && (
          <span className="text-sm font-normal text-muted-foreground">
            <Trans>Excl. tax</Trans>
          </span>
        )}
      </div>
      <Separator />
      <div className="grid grid-cols-2 gap-2 lg:grid-cols-1">
        {details.features.map((feature) => (
          <div key={feature} className="flex items-center gap-2 text-sm">
            <CheckIcon className="size-4 shrink-0 text-primary" />
            <span>{feature}</span>
          </div>
        ))}
      </div>
      <div className="mt-auto pt-2">{renderAction()}</div>
    </div>
  );
}
