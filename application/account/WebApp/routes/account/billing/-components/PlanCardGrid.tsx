import type { components } from "@/shared/lib/api/api.generated";

import { type SubscriptionPlan, SubscriptionPlan as Plans } from "@/shared/lib/api/client";

import { getCatalogUnitAmount, getFormattedPrice, PlanCard } from "./PlanCard";

type PlanPriceItem = components["schemas"]["PlanPriceItem"];

interface PlanCardGridProps {
  plans: PlanPriceItem[] | undefined;
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
  currentPriceAmount: number | null | undefined;
  currentPriceCurrency: string | null | undefined;
}

export function PlanCardGrid({
  plans,
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
  currentPriceCurrency
}: Readonly<PlanCardGridProps>) {
  return (
    <div className="grid gap-4 lg:grid-cols-3">
      {[Plans.Basis, Plans.Standard, Plans.Premium].map((plan) => {
        const planItem = plans?.find((p) => p.plan === plan);
        const taxExclusive = planItem != null && !planItem.taxInclusive;
        return (
          <PlanCard
            key={plan}
            plan={plan}
            formattedPrice={getFormattedPrice(plan, plans)}
            currentPlan={currentPlan}
            cancelAtPeriodEnd={cancelAtPeriodEnd}
            scheduledPlan={scheduledPlan}
            isStripeConfigured={isStripeConfigured}
            onSubscribe={onSubscribe}
            onUpgrade={onUpgrade}
            onDowngrade={onDowngrade}
            onReactivate={onReactivate}
            onCancelDowngrade={onCancelDowngrade}
            isPending={isPending}
            pendingPlan={pendingPlan}
            isCancelDowngradePending={isCancelDowngradePending}
            currentPriceAmount={currentPriceAmount}
            currentPriceCurrency={currentPriceCurrency}
            catalogUnitAmount={getCatalogUnitAmount(plan, plans)}
            taxExclusive={taxExclusive}
          />
        );
      })}
    </div>
  );
}
