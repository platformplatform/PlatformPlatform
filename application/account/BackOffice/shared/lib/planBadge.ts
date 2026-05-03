import { SubscriptionPlan } from "@/shared/lib/api/client";

export function getSubscriptionPlanBadgeClass(plan: SubscriptionPlan): string {
  switch (plan) {
    case SubscriptionPlan.Basis:
      return "border-transparent bg-muted text-foreground";
    case SubscriptionPlan.Standard:
      return "border-transparent bg-blue-100 text-blue-700 dark:bg-blue-500/20 dark:text-blue-300";
    case SubscriptionPlan.Premium:
      return "border-transparent bg-amber-100 text-amber-800 dark:bg-amber-500/20 dark:text-amber-300";
    default:
      return "";
  }
}
