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

    public decimal? PreviousAmount { get; private set; }

    public decimal? NewAmount { get; private set; }

    public decimal? AmountDelta { get; private set; }

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
