import { BillingEventType, SubscriptionPlan } from "@/shared/lib/api/client";

/**
 * Events whose semantics include a plan transition (subscription state changes plus MRR-only events
 * that still carry a plan). The Billing Events table renders the `{fromPlan} → {toPlan}` column for
 * these types; every other event renders the column empty. Used by both the cross-account billing
 * events route and the per-tenant Billing events tab.
 */
export const PLAN_TRANSITION_EVENT_TYPES: ReadonlySet<BillingEventType> = new Set([
  BillingEventType.SubscriptionCreated,
  BillingEventType.SubscriptionRenewed,
  BillingEventType.SubscriptionUpgraded,
  BillingEventType.SubscriptionDowngradeScheduled,
  BillingEventType.SubscriptionDowngradeCancelled,
  BillingEventType.SubscriptionDowngraded,
  BillingEventType.SubscriptionReactivated,
  BillingEventType.SubscriptionExpired,
  BillingEventType.SubscriptionImmediatelyCancelled,
  BillingEventType.SubscriptionSuspended,
  BillingEventType.SubscriptionCancelled,
  BillingEventType.PaymentRefunded
]);

/** Default `fromPlan` when an event's persisted `fromPlan` is null (e.g. SubscriptionCreated). */
export const DEFAULT_FROM_PLAN = SubscriptionPlan.Basis;

/**
 * Event types whose primary signal is "revenue moved". Powers the "MRR impact" pill on the Billing
 * Events filter toolbar. SubscriptionCreated belongs here because the first subscription is the
 * first MRR event — the customer began contributing revenue.
 */
export const MRR_IMPACT_EVENT_TYPES: readonly BillingEventType[] = [
  BillingEventType.SubscriptionCreated,
  BillingEventType.SubscriptionUpgraded,
  BillingEventType.SubscriptionDowngradeScheduled,
  BillingEventType.SubscriptionDowngradeCancelled,
  BillingEventType.SubscriptionDowngraded,
  BillingEventType.SubscriptionImmediatelyCancelled,
  BillingEventType.PaymentRefunded
];

/**
 * Event types whose primary signal is "the subscription's lifecycle changed". Powers the
 * "Subscription state" pill. Reactivated lives here because the state transition is dominant —
 * the MRR uptick is a consequence.
 */
export const SUBSCRIPTION_STATE_EVENT_TYPES: readonly BillingEventType[] = [
  BillingEventType.SubscriptionRenewed,
  BillingEventType.SubscriptionReactivated,
  BillingEventType.SubscriptionExpired,
  BillingEventType.SubscriptionSuspended,
  BillingEventType.SubscriptionCancelled
];

/**
 * Event types that don't fit either MRR impact or Subscription state — payment hiccups, billing
 * metadata changes, audit catch-alls. Grouped under "Other" in the filter dropdown.
 */
export const OTHER_EVENT_TYPES: readonly BillingEventType[] = [
  BillingEventType.SubscriptionPastDue,
  BillingEventType.PaymentFailed,
  BillingEventType.PaymentRecovered,
  BillingEventType.BillingInfoAdded,
  BillingEventType.BillingInfoUpdated,
  BillingEventType.PaymentMethodUpdated,
  BillingEventType.NoOp,
  BillingEventType.Unclassified
];
