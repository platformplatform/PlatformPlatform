import { BillingEventType, SubscriptionPlan } from "@/shared/lib/api/client";

import { DEFAULT_FROM_PLAN } from "./billingEventCategories";

export type PlanTransition = { from: SubscriptionPlan; to: SubscriptionPlan };

// SubscriptionCancelled is persisted with from_plan=null and to_plan=<the plan that was cancelled>.
// Rendered with the default-when-null logic that becomes "Basis → Standard", which reads as the
// opposite of a cancellation. Flip for display so the cancelled plan is shown on the left.
export function getDisplayedPlanTransition(
  eventType: BillingEventType,
  fromPlan: SubscriptionPlan | null | undefined,
  toPlan: SubscriptionPlan | null | undefined
): PlanTransition | null {
  if (toPlan == null) return null;
  if (eventType === BillingEventType.SubscriptionCancelled) {
    return { from: toPlan, to: SubscriptionPlan.Basis };
  }
  return { from: fromPlan ?? DEFAULT_FROM_PLAN, to: toPlan };
}
