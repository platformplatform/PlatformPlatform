import { t } from "@lingui/core/macro";

import {
  DeviceType,
  LoginMethod,
  PaymentTransactionStatus,
  PlannedSubscriptionChange,
  StripeEventType,
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

export function getDeviceTypeLabel(deviceType: DeviceType): string {
  switch (deviceType) {
    case DeviceType.Desktop:
      return t`Desktop`;
    case DeviceType.Mobile:
      return t`Mobile`;
    case DeviceType.Tablet:
      return t`Tablet`;
    case DeviceType.Unknown:
      return t`Unknown`;
    default:
      return String(deviceType);
  }
}

export function getLoginMethodLabel(method: LoginMethod): string {
  switch (method) {
    case LoginMethod.OneTimePassword:
      return t`One-time password`;
    case LoginMethod.Google:
      return t`Google`;
    default:
      return String(method);
  }
}

export function getStripeEventTypeLabel(type: StripeEventType): string {
  switch (type) {
    case StripeEventType.Subscribed:
      return t`Subscribed`;
    case StripeEventType.Upgraded:
      return t`Upgraded`;
    case StripeEventType.Downgraded:
      return t`Downgraded`;
    case StripeEventType.Canceled:
      return t`Canceled`;
    case StripeEventType.PaymentFailed:
      return t`Payment failed`;
    default:
      return String(type);
  }
}
