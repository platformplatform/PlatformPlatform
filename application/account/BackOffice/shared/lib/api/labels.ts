import { t } from "@lingui/core/macro";

import {
  BillingEventType,
  DeviceType,
  LoginMethod,
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
      return t`Paid`;
    case PaymentTransactionStatus.Failed:
      return t`Failed`;
    case PaymentTransactionStatus.Pending:
      return t`Pending`;
    case PaymentTransactionStatus.Refunded:
      return t`Refunded`;
    case PaymentTransactionStatus.Cancelled:
      return t`Cancelled`;
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

export function getBillingEventTypeLabel(type: BillingEventType): string {
  switch (type) {
    case BillingEventType.SubscriptionCreated:
      return t`Subscribed`;
    case BillingEventType.SubscriptionRenewed:
      return t`Renewed`;
    case BillingEventType.SubscriptionUpgraded:
      return t`Upgraded`;
    case BillingEventType.SubscriptionDowngradeScheduled:
      return t`Downgrade scheduled`;
    case BillingEventType.SubscriptionDowngradeCancelled:
      return t`Downgrade cancelled`;
    case BillingEventType.SubscriptionDowngraded:
      return t`Downgraded`;
    case BillingEventType.SubscriptionCancelled:
      return t`Cancelled`;
    case BillingEventType.SubscriptionReactivated:
      return t`Reactivated`;
    case BillingEventType.SubscriptionExpired:
      return t`Expired`;
    case BillingEventType.SubscriptionImmediatelyCancelled:
      return t`Cancelled immediately`;
    case BillingEventType.SubscriptionSuspended:
      return t`Suspended`;
    case BillingEventType.SubscriptionPastDue:
      return t`Past due`;
    case BillingEventType.PaymentFailed:
      return t`Payment failed`;
    case BillingEventType.PaymentRecovered:
      return t`Payment recovered`;
    case BillingEventType.PaymentRefunded:
      return t`Payment refunded`;
    case BillingEventType.BillingInfoAdded:
      return t`Billing info added`;
    case BillingEventType.BillingInfoUpdated:
      return t`Billing info updated`;
    case BillingEventType.PaymentMethodUpdated:
      return t`Payment method updated`;
    case BillingEventType.NoOp:
      return t`No change`;
    case BillingEventType.Unclassified:
      return t`Unclassified`;
    default:
      return String(type);
  }
}
