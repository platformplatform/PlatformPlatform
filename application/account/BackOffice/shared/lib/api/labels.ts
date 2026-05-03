import { t } from "@lingui/core/macro";

import {
  PaymentTransactionStatus,
  PlannedSubscriptionChange,
  SubscriptionPlan,
  TenantState,
  UserRole
} from "@/shared/lib/api/client";

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

export function getPlannedChangeLabel(change: PlannedSubscriptionChange): string {
  switch (change) {
    case PlannedSubscriptionChange.Cancellation:
      return t`Cancellation`;
    case PlannedSubscriptionChange.ScheduledPlanChange:
      return t`Scheduled plan change`;
    default:
      return String(change);
  }
}

export function getTenantStateLabel(state: TenantState): string {
  switch (state) {
    case TenantState.Active:
      return t`Active`;
    case TenantState.Suspended:
      return t`Suspended`;
    default:
      return String(state);
  }
}

export function getPaymentStatusLabel(status: PaymentTransactionStatus): string {
  switch (status) {
    case PaymentTransactionStatus.Succeeded:
      return t`Succeeded`;
    case PaymentTransactionStatus.Failed:
      return t`Failed`;
    case PaymentTransactionStatus.Pending:
      return t`Pending`;
    case PaymentTransactionStatus.Refunded:
      return t`Refunded`;
    default:
      return String(status);
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
