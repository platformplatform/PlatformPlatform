import { t } from "@lingui/core/macro";

import { SubscriptionPlan, UserRole } from "@/shared/lib/api/client";

export function getSubscriptionPlanLabel(plan: SubscriptionPlan): string {
  switch (plan) {
    case SubscriptionPlan.Basis:
      return t`Basis`;
    case SubscriptionPlan.Standard:
      return t`Standard`;
    case SubscriptionPlan.Premium:
      return t`Premium`;
    default:
      return String(plan);
  }
}

export function getUserRoleLabel(role: UserRole): string {
  switch (role) {
    case UserRole.Owner:
      return t`Owner`;
    case UserRole.Admin:
      return t`Admin`;
    case UserRole.Member:
      return t`Member`;
    default:
      return String(role);
  }
}
