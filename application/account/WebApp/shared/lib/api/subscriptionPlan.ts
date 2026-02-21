import { t } from "@lingui/core/macro";
import { SubscriptionPlan } from "@/shared/lib/api/client";

export function getPlanLabel(plan: SubscriptionPlan): string {
  switch (plan) {
    case SubscriptionPlan.Basis:
      return t`Basis`;
    case SubscriptionPlan.Standard:
      return t`Standard`;
    case SubscriptionPlan.Premium:
      return t`Premium`;
  }
}

export function getPlanLabelWithFree(plan: string | undefined): string {
  if (plan && Object.values(SubscriptionPlan).includes(plan as SubscriptionPlan)) {
    return getPlanLabel(plan as SubscriptionPlan);
  }
  return t`Free`;
}
