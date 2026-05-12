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
 * Event types that change recurring revenue. Powers the "MRR impact" pill on the Billing Events
 * filter toolbar. Includes every subscription transition that moves committed MRR up or down —
 * Subscribed/Upgraded/Reactivated raise it, Cancelled/Downgraded/Expired/ImmediatelyCancelled
 * lower it. Downgrade schedule/cancel preview a future MRR change so they belong here too.
 * Events can appear in more than one category (an Upgrade also transitions state) — the pill
 * filters are independent lenses, not a partition.
 */
export const MRR_IMPACT_EVENT_TYPES: readonly BillingEventType[] = [
  BillingEventType.SubscriptionCreated,
  BillingEventType.SubscriptionUpgraded,
  BillingEventType.SubscriptionDowngradeScheduled,
  BillingEventType.SubscriptionDowngradeCancelled,
  BillingEventType.SubscriptionDowngraded,
  BillingEventType.SubscriptionReactivated,
  BillingEventType.SubscriptionCancelled,
  BillingEventType.SubscriptionExpired,
  BillingEventType.SubscriptionImmediatelyCancelled
];

/**
 * Event types that change the subscription's effective plan or terminal state. Powers the
 * "Subscription state" pill. Strict semantics: a row qualifies only when the plan the customer
 * is currently on actually changes (immediately or by the event taking effect). Excludes
 * scheduled future changes (DowngradeScheduled), reversals of scheduled changes
 * (DowngradeCancelled, Reactivated — the effective plan didn't move), soft cancels that don't
 * take effect until period end (Cancelled — the customer is still on the same plan), and
 * Renewed (same plan, new period). Overlaps with MRR impact for any state transition that
 * also moves committed revenue.
 */
export const SUBSCRIPTION_STATE_EVENT_TYPES: readonly BillingEventType[] = [
  BillingEventType.SubscriptionCreated,
  BillingEventType.SubscriptionUpgraded,
  BillingEventType.SubscriptionDowngraded,
  BillingEventType.SubscriptionExpired,
  BillingEventType.SubscriptionImmediatelyCancelled,
  BillingEventType.SubscriptionSuspended
];

/**
 * Payment-flow, billing-metadata, and same-plan-renewal events that neither change committed MRR
 * nor mutate the subscription's effective plan. Renewed reuses the same plan into a new period
 * (no plan change). Refunds are a backwards money flow on an existing charge. Past-due/failed/
 * recovered are payment hiccups, not state transitions on the Subscription aggregate. Grouped
 * under "Other".
 */
export const OTHER_EVENT_TYPES: readonly BillingEventType[] = [
  BillingEventType.SubscriptionRenewed,
  BillingEventType.SubscriptionPastDue,
  BillingEventType.PaymentFailed,
  BillingEventType.PaymentRecovered,
  BillingEventType.PaymentRefunded,
  BillingEventType.BillingInfoAdded,
  BillingEventType.BillingInfoUpdated,
  BillingEventType.PaymentMethodUpdated,
  BillingEventType.NoOp,
  BillingEventType.Unclassified
];
