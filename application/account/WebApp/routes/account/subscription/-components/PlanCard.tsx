import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { Badge } from "@repo/ui/components/Badge";
import { Button } from "@repo/ui/components/Button";
import { Separator } from "@repo/ui/components/Separator";
import { CheckIcon } from "lucide-react";
import { SubscriptionPlan } from "@/shared/lib/api/client";

type PlanCardProps = {
  plan: SubscriptionPlan;
  currentPlan: SubscriptionPlan;
  cancelAtPeriodEnd: boolean;
  scheduledPlan: SubscriptionPlan | null;
  isStripeConfigured: boolean;
  onSubscribe: (plan: SubscriptionPlan) => void;
  onUpgrade: (plan: SubscriptionPlan) => void;
  onDowngrade: (plan: SubscriptionPlan) => void;
  onReactivate: (plan: SubscriptionPlan) => void;
  onCancelDowngrade: () => void;
  isPending: boolean;
  pendingPlan: SubscriptionPlan | null;
  isCancelDowngradePending: boolean;
};

type PlanDetails = {
  name: string;
  price: string;
  features: string[];
};

export function getPlanDetails(plan: SubscriptionPlan): PlanDetails {
  switch (plan) {
    case SubscriptionPlan.Basis:
      return {
        name: t`Basis`,
        price: t`Free`,
        features: [t`5 users`, t`10 GB storage`, t`Basic support`]
      };
    case SubscriptionPlan.Standard:
      return {
        name: t`Standard`,
        price: t`EUR 19/month`,
        features: [t`10 users`, t`100 GB storage`, t`Email support`, t`Analytics`]
      };
    case SubscriptionPlan.Premium:
      return {
        name: t`Premium`,
        price: t`EUR 39/month`,
        features: [t`Unlimited users`, t`1 TB storage`, t`Priority support`, t`Advanced analytics`, t`SLA`]
      };
  }
}

function getPlanOrder(plan: SubscriptionPlan): number {
  switch (plan) {
    case SubscriptionPlan.Basis:
      return 0;
    case SubscriptionPlan.Standard:
      return 1;
    case SubscriptionPlan.Premium:
      return 2;
  }
}

export function PlanCard({
  plan,
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
  isCancelDowngradePending
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
      if (plan === SubscriptionPlan.Basis) {
        return null;
      }
      return (
        <Button
          variant="default"
          className="w-full"
          onClick={() => onReactivate(plan)}
          disabled={isPending || !isStripeConfigured}
        >
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

    if (isDowngrade && plan !== SubscriptionPlan.Basis) {
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
      <div className="font-semibold text-2xl">{details.price}</div>
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
