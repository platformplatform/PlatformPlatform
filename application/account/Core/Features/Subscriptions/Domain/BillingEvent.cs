using Account.Features.Tenants.Domain;
using JetBrains.Annotations;
using SharedKernel.Domain;
using SharedKernel.StronglyTypedIds;

namespace Account.Features.Subscriptions.Domain;

[PublicAPI]
[IdPrefix("bilevt")]
[JsonConverter(typeof(StronglyTypedIdJsonConverter<string, BillingEventId>))]
public sealed record BillingEventId(string Value) : StronglyTypedUlid<BillingEventId>(Value)
{
    public override string ToString()
    {
        return Value;
    }
}

/// <summary>
///     A durable, append-only record of one subscription-relevant Stripe event.
///     The invariant is strict 1:1: every recognized Stripe event for a subscription produces exactly
///     one row. Events that don't move state we care about are written as <see cref="BillingEventType.NoOp" />.
///     Events whose Stripe payload combines multiple changes that don't decompose into one of our domain
///     transitions (e.g. a subscription update that toggles cancel-at-period-end *and* changes price in
///     the same payload) are written as <see cref="BillingEventType.Unclassified" /> and flip the
///     subscription's drift flag for admin review.
///     Idempotent on <see cref="StripeEventId" /> (unique index): redelivered webhooks and re-pulls from
///     the Stripe events API are no-ops.
///     Source of truth: the local <c>stripe_events</c> archive, NOT Stripe's events.list API. Stripe only
///     retains events for 30 days (see https://docs.stripe.com/api/events) — anything older must come from
///     our local archive. The events.list API is used only as a reconciliation source for detecting
///     webhooks that never reached us within the retention window.
///     Hard rule: rows in this table are never deleted, never updated. Schema changes use ALTER TABLE
///     ADD/DROP COLUMN, never DROP/TRUNCATE/DELETE FROM.
/// </summary>
public sealed class BillingEvent : AggregateRoot<BillingEventId>, ITenantScopedEntity
{
    private BillingEvent(TenantId tenantId, SubscriptionId subscriptionId, string stripeEventId)
        : base(BillingEventId.NewId())
    {
        TenantId = tenantId;
        SubscriptionId = subscriptionId;
        StripeEventId = stripeEventId;
        EventType = default;
        OccurredAt = default;
    }

    public SubscriptionId SubscriptionId { get; private set; }

    public string StripeEventId { get; private set; }

    public BillingEventType EventType { get; private set; }

    public SubscriptionPlan? FromPlan { get; private set; }

    public SubscriptionPlan? ToPlan { get; private set; }

    /// <summary>
    ///     The ex-VAT recurring price before this event applied. ALWAYS ex-VAT: MRR is revenue
    ///     accounting, VAT is collected on behalf of tax authorities and never our revenue, so every
    ///     amount column on this aggregate is net-of-tax. Null for event types that don't carry a
    ///     before/after price (e.g. <see cref="BillingEventType.BillingInfoUpdated" />).
    /// </summary>
    public decimal? PreviousAmount { get; private set; }

    /// <summary>
    ///     The ex-VAT recurring price after this event applied. ALWAYS ex-VAT: MRR is revenue
    ///     accounting, VAT is collected on behalf of tax authorities and never our revenue, so every
    ///     amount column on this aggregate is net-of-tax. Sourced either from the Stripe event payload's
    ///     <c>unit_amount</c> (normalized to ex-VAT when the underlying price's <c>tax_behavior</c> is
    ///     <c>inclusive</c>) or from the ex-VAT catalog fallback. Null for event types that don't
    ///     carry a new price.
    /// </summary>
    public decimal? NewAmount { get; private set; }

    /// <summary>
    ///     The ex-VAT difference <see cref="NewAmount" /> − <see cref="PreviousAmount" />, signed.
    ///     ALWAYS ex-VAT: MRR is revenue accounting, VAT is collected on behalf of tax authorities and
    ///     never our revenue, so the recurring-revenue delta is net-of-tax. Null for event types where
    ///     a delta is undefined (NoOp, BillingInfoUpdated, PaymentMethodUpdated, etc.).
    /// </summary>
    public decimal? AmountDelta { get; private set; }

    /// <summary>
    ///     The ex-VAT committed MRR state immediately after this event applied — the running total
    ///     denormalized so paginated reads don't have to walk history. ALWAYS ex-VAT: MRR is revenue
    ///     accounting, VAT is collected on behalf of tax authorities and never our revenue, so the
    ///     KPI sum must be net-of-tax. Zero when the subscription is cancelled-at-period-end (the
    ///     forward MRR contribution drops at the moment the customer commits to leaving, not at the
    ///     effective period end).
    /// </summary>
    public decimal CommittedMrr { get; private set; }

    public string? Currency { get; private set; }

    public DateTimeOffset OccurredAt { get; private set; }

    public CancellationReason? CancellationReason { get; private set; }

    public SuspensionReason? SuspensionReason { get; private set; }

    public TenantId TenantId { get; }

    public static BillingEvent Create(
        TenantId tenantId,
        SubscriptionId subscriptionId,
        string stripeEventId,
        BillingEventType eventType,
        DateTimeOffset occurredAt,
        decimal committedMrr,
        SubscriptionPlan? fromPlan = null,
        SubscriptionPlan? toPlan = null,
        decimal? previousAmount = null,
        decimal? newAmount = null,
        decimal? amountDelta = null,
        string? currency = null,
        CancellationReason? cancellationReason = null,
        SuspensionReason? suspensionReason = null
    )
    {
        if (committedMrr < 0m)
        {
            throw new UnreachableException($"BillingEvent.committedMrr must be non-negative; got {committedMrr} for event '{stripeEventId}'.");
        }

        if (previousAmount is not null && newAmount is not null && amountDelta is not null
            && Math.Abs(newAmount.Value - previousAmount.Value - amountDelta.Value) > 0.005m)
        {
            throw new UnreachableException($"BillingEvent.amountDelta inconsistency on event '{stripeEventId}': previousAmount={previousAmount}, newAmount={newAmount}, amountDelta={amountDelta}.");
        }

        return new BillingEvent(tenantId, subscriptionId, stripeEventId)
        {
            EventType = eventType,
            OccurredAt = occurredAt,
            CommittedMrr = committedMrr,
            FromPlan = fromPlan,
            ToPlan = toPlan,
            PreviousAmount = previousAmount,
            NewAmount = newAmount,
            AmountDelta = amountDelta,
            Currency = currency,
            CancellationReason = cancellationReason,
            SuspensionReason = suspensionReason
        };
    }
}

/// <summary>
///     The type of subscription-relevant Stripe event recorded by the BillingEvent log.
///     IMPORTANT: when adding a new value, also add it to the multi-select on /billing-events
///     (see <c>application/account/BackOffice/routes/billing-events/-components/BillingEventsToolbar.tsx</c>,
///     constant <c>ALL_EVENT_TYPES</c>). The toolbar is hand-maintained and does not enumerate the
///     enum at runtime — operators won't be able to filter by a new type until that list is updated.
/// </summary>
[PublicAPI]
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum BillingEventType
{
    SubscriptionCreated,
    SubscriptionRenewed,
    SubscriptionUpgraded,
    SubscriptionDowngradeScheduled,
    SubscriptionDowngradeCancelled,
    SubscriptionDowngraded,
    SubscriptionCancelled,
    SubscriptionReactivated,
    SubscriptionExpired,
    SubscriptionImmediatelyCancelled,
    SubscriptionSuspended,

    /// <summary>
    ///     Stripe transitioned the subscription's status from active to past_due (a payment failed).
    ///     Fires alongside <see cref="PaymentFailed" /> from the corresponding invoice.payment_failed event;
    ///     pairs with <see cref="SubscriptionReactivated" /> when payment recovers and status returns to active.
    ///     Carries forward CommittedMrr unchanged and AmountDelta=null — the customer is still on the plan,
    ///     just behind on payment.
    /// </summary>
    SubscriptionPastDue,
    PaymentFailed,
    PaymentRecovered,
    PaymentRefunded,
    BillingInfoAdded,
    BillingInfoUpdated,
    PaymentMethodUpdated,

    /// <summary>
    ///     A recognized subscription-relevant Stripe event that doesn't move state we care about (e.g.
    ///     a subscription_schedule.updated arriving with status=canceled after a cancellation, where
    ///     phases haven't changed). Hidden from the timeline UI; carries forward CommittedMrr unchanged
    ///     and AmountDelta=null so it's invisible to MRR trend computation.
    /// </summary>
    NoOp,

    /// <summary>
    ///     A Stripe event whose payload combines multiple state changes that the writer can't decompose
    ///     into a single domain transition (e.g. a customer.subscription.updated whose previous_attributes
    ///     contain both a cancel_at_period_end toggle and a price change). Triggers the drift banner so
    ///     an admin can investigate in Stripe Dashboard.
    /// </summary>
    Unclassified
}
