using System.Collections.Immutable;
using JetBrains.Annotations;
using SharedKernel.Domain;
using SharedKernel.StronglyTypedIds;

namespace Account.Features.Subscriptions.Domain;

[PublicAPI]
[IdPrefix("sub")]
[JsonConverter(typeof(StronglyTypedIdJsonConverter<string, SubscriptionId>))]
public sealed record SubscriptionId(string Value) : StronglyTypedUlid<SubscriptionId>(Value)
{
    public override string ToString()
    {
        return Value;
    }
}

[PublicAPI]
[IdPrefix("pymnt")]
[JsonConverter(typeof(StronglyTypedIdJsonConverter<string, PaymentTransactionId>))]
public sealed record PaymentTransactionId(string Value) : StronglyTypedUlid<PaymentTransactionId>(Value)
{
    public override string ToString()
    {
        return Value;
    }
}

public sealed class Subscription : AggregateRoot<SubscriptionId>, ITenantScopedEntity
{
    private Subscription(TenantId tenantId) : base(SubscriptionId.NewId())
    {
        TenantId = tenantId;
        Plan = SubscriptionPlan.Basis;
        PaymentTransactions = ImmutableArray<PaymentTransaction>.Empty;
        DriftDiscrepancies = ImmutableArray<DriftDiscrepancy>.Empty;
    }

    public SubscriptionPlan Plan { get; private set; }

    public SubscriptionPlan? ScheduledPlan { get; private set; }

    public decimal? ScheduledPriceAmount { get; private set; }

    public StripeCustomerId? StripeCustomerId { get; private set; }

    public StripeSubscriptionId? StripeSubscriptionId { get; private set; }

    public decimal? CurrentPriceAmount { get; private set; }

    public string? CurrentPriceCurrency { get; private set; }

    public DateTimeOffset? CurrentPeriodEnd { get; private set; }

    public bool CancelAtPeriodEnd { get; private set; }

    public DateTimeOffset? FirstPaymentFailedAt { get; private set; }

    public CancellationReason? CancellationReason { get; private set; }

    public string? CancellationFeedback { get; private set; }

    public DateTimeOffset? SubscribedSince { get; private set; }

    /// <summary>
    ///     The <c>Event.Created</c> of the most recent Stripe event applied to this subscription via the
    ///     events.list-driven hot path. Used as the <c>created.gte</c> anchor on the next sync so we only
    ///     fetch events Stripe has produced since we were last in sync. Stripe retains events for 30 days
    ///     (see https://docs.stripe.com/api/events); the background sweeper re-syncs every active customer
    ///     well within that window so this anchor never falls out of range. Null on subscriptions that
    ///     have never been synced (e.g. fresh tenants on Basis).
    /// </summary>
    public DateTimeOffset? LastSyncedStripeEventCreatedAt { get; private set; }

    public ImmutableArray<PaymentTransaction> PaymentTransactions { get; private set; }

    public PaymentMethod? PaymentMethod { get; private set; }

    public BillingInfo? BillingInfo { get; private set; }

    public bool HasDriftDetected { get; private set; }

    public DateTimeOffset? DriftCheckedAt { get; private set; }

    public ImmutableArray<DriftDiscrepancy> DriftDiscrepancies { get; private set; }

    public TenantId TenantId { get; }

    public static Subscription Create(TenantId tenantId)
    {
        return new Subscription(tenantId);
    }

    public void SetStripeCustomerId(StripeCustomerId stripeCustomerId)
    {
        StripeCustomerId = stripeCustomerId;
    }

    public void SetBillingInfo(BillingInfo? billingInfo)
    {
        BillingInfo = billingInfo;
    }

    public void SetStripeSubscription(StripeSubscriptionId? stripeSubscriptionId, SubscriptionPlan plan, decimal? currentPriceAmount, string? currentPriceCurrency, DateTimeOffset? currentPeriodEnd, PaymentMethod? paymentMethod, DateTimeOffset now)
    {
        var previousPlan = Plan;

        StripeSubscriptionId = stripeSubscriptionId;
        Plan = plan;
        CurrentPriceAmount = currentPriceAmount;
        CurrentPriceCurrency = currentPriceCurrency;
        CurrentPeriodEnd = currentPeriodEnd;
        PaymentMethod = paymentMethod;

        // Capture the start of a paid run only when transitioning from Basis (free) to a paid plan.
        // Plan changes between paid plans (e.g., Standard <-> Premium) preserve the original SubscribedSince.
        if (previousPlan == SubscriptionPlan.Basis && plan != SubscriptionPlan.Basis)
        {
            SubscribedSince = now;
        }
    }

    /// <summary>
    ///     Authoritative Stripe <c>Customer.Created</c> value. Supersedes the migration backfill of
    ///     <c>created_at</c>. Called on every sync so the tenant's <c>SubscribedSince</c> converges
    ///     to Stripe's customer-creation timestamp regardless of any earlier local value.
    /// </summary>
    public void SetSubscribedSinceFromStripe(DateTimeOffset stripeCustomerCreated)
    {
        SubscribedSince = stripeCustomerCreated;
    }

    /// <summary>
    ///     Advances the events.list anchor to the <c>Event.Created</c> of the most recent event applied in
    ///     this sync. Monotonic: only advances forward so a late-arriving older event recovered via
    ///     reconcile cannot rewind the anchor below an already-applied event.
    /// </summary>
    public void AdvanceLastSyncedStripeEventCreatedAt(DateTimeOffset eventCreatedAt)
    {
        if (LastSyncedStripeEventCreatedAt is null || eventCreatedAt > LastSyncedStripeEventCreatedAt.Value)
        {
            LastSyncedStripeEventCreatedAt = eventCreatedAt;
        }
    }

    public void SetCancellation(bool cancelAtPeriodEnd, CancellationReason? cancellationReason, string? cancellationFeedback)
    {
        CancelAtPeriodEnd = cancelAtPeriodEnd;
        CancellationReason = cancellationReason;
        CancellationFeedback = cancellationFeedback;
    }

    public void SetScheduledPlan(SubscriptionPlan? scheduledPlan, decimal? scheduledPriceAmount)
    {
        ScheduledPlan = scheduledPlan;
        ScheduledPriceAmount = scheduledPriceAmount;
    }

    public void SetPaymentTransactions(ImmutableArray<PaymentTransaction> paymentTransactions)
    {
        PaymentTransactions = paymentTransactions;
    }

    public void SetPaymentMethod(PaymentMethod? paymentMethod)
    {
        PaymentMethod = paymentMethod;
    }

    public void SetPaymentFailed(DateTimeOffset failedAt)
    {
        FirstPaymentFailedAt = failedAt;
    }

    public void ClearPaymentFailure()
    {
        FirstPaymentFailedAt = null;
    }

    public void ResetToFreePlan()
    {
        Plan = SubscriptionPlan.Basis;
        ScheduledPlan = null;
        ScheduledPriceAmount = null;
        StripeSubscriptionId = null;
        CurrentPriceAmount = null;
        CurrentPriceCurrency = null;
        CurrentPeriodEnd = null;
        CancelAtPeriodEnd = false;
        FirstPaymentFailedAt = null;
        CancellationReason = null;
        CancellationFeedback = null;
        SubscribedSince = null;
    }

    public bool HasActiveStripeSubscription()
    {
        return StripeSubscriptionId is not null && Plan != SubscriptionPlan.Basis && !CancelAtPeriodEnd;
    }

    public void SetDriftStatus(ImmutableArray<DriftDiscrepancy> discrepancies, DateTimeOffset checkedAt)
    {
        DriftDiscrepancies = discrepancies;
        HasDriftDetected = !discrepancies.IsDefaultOrEmpty;
        DriftCheckedAt = checkedAt;
    }

    public void AcknowledgeDrift(DateTimeOffset acknowledgedAt)
    {
        // Manual override clears the flag but preserves the discrepancy list for audit.
        HasDriftDetected = false;
        DriftCheckedAt = acknowledgedAt;
    }
}

[PublicAPI]
public sealed record BillingAddress(
    string? Line1,
    string? Line2,
    string? PostalCode,
    string? City,
    string? State,
    string? Country
);

[PublicAPI]
public sealed record BillingInfo(string? Name, BillingAddress? Address, string? Email, string? TaxId);

[PublicAPI]
public sealed record PaymentMethod(string Brand, string Last4, int ExpMonth, int ExpYear);

[PublicAPI]
public sealed record PaymentTransaction(
    PaymentTransactionId Id,
    decimal Amount,
    decimal AmountExcludingTax,
    decimal TaxAmount,
    string Currency,
    PaymentTransactionStatus Status,
    DateTimeOffset Date,
    string? FailureReason,
    string? InvoiceUrl,
    string? CreditNoteUrl,
    SubscriptionPlan? Plan = null,
    DateTimeOffset? RefundedAt = null
);

[PublicAPI]
public sealed record DriftDiscrepancy(
    DriftDiscrepancyKind Kind,
    string Description,
    DriftSeverity Severity,
    BillingEventType? ExpectedEventType = null,
    string? ExpectedValue = null,
    string? ActualValue = null,
    DateTimeOffset? OccurredAt = null
);

[PublicAPI]
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum DriftDiscrepancyKind
{
    MissingEvent,
    ExtraEvent,
    FieldDisagree,
    SubscriptionStateMismatch,

    /// <summary>
    ///     A Stripe event arrived whose payload combined multiple state changes that the writer couldn't
    ///     decompose into a single domain transition (e.g. a customer.subscription.updated whose
    ///     previous_attributes contain both a cancel_at_period_end toggle and a price change). The
    ///     event is recorded as <c>BillingEventType.Unclassified</c>; this discrepancy surfaces it on
    ///     the drift banner so an admin can investigate in Stripe Dashboard.
    /// </summary>
    UnclassifiedStripeEvent,

    /// <summary>
    ///     A subscription resource (payment transaction, schedule, etc.) implies a Stripe event
    ///     should exist in our archive but doesn't. The event is still within Stripe's 30-day
    ///     events.list retention window, so the next reconciliation pass should automatically
    ///     recover it. The drift banner shows a countdown of the remaining time before this
    ///     escalates to <see cref="MissingHistoricalEventUnrecoverable" />.
    /// </summary>
    MissingHistoricalEvent,

    /// <summary>
    ///     A subscription resource implies a Stripe event should exist in our archive but doesn't,
    ///     and Stripe's 30-day events.list retention window has closed. The data is permanently
    ///     lost from Stripe — escalates to a P1 incident on the drift banner so the missed
    ///     reconciliation can be investigated and the underlying bug fixed.
    /// </summary>
    MissingHistoricalEventUnrecoverable,

    /// <summary>
    ///     Stripe sent an event whose <c>api_version</c> doesn't have a matching
    ///     <c>IStripeEventPayloadResolver</c>. The event is preserved unchanged in
    ///     <c>stripe_events</c>; the replayer skips it and surfaces this discrepancy so the
    ///     resolver-per-version mapping can be extended.
    /// </summary>
    UnsupportedStripeApiVersion,

    /// <summary>
    ///     The same Stripe event id was observed twice with different payloads (SHA-256 hash
    ///     mismatch on the second arrival). The original row is preserved; the divergence is
    ///     surfaced for forensic review. Either Stripe redelivered an event with mutated content
    ///     (their bug to investigate) or our hashing is broken (our bug to investigate).
    /// </summary>
    StripeEventPayloadDivergence,

    /// <summary>
    ///     A persisted BillingEvent row's denormalized fields (CommittedMrr, AmountDelta, PreviousAmount, NewAmount)
    ///     no longer match what a fresh replay produces — typically because an older event was recovered after a
    ///     newer event was already classified and persisted. The persisted row is left untouched per the
    ///     append-only invariant; this discrepancy surfaces the wrongness for operator review.
    /// </summary>
    BillingEventDenormalizationStale,

    /// <summary>
    ///     The subscription has a <c>ScheduledPlan</c> set but <c>ScheduledPriceAmount</c> is null. The
    ///     MRR KPI falls back to the current (higher) price in this state, silently distorting BLENDED MRR.
    ///     Originates from edge cases in <c>SyncStateFromStripe</c> where a cancel-then-reschedule pair
    ///     landed in the same sync window and the diff-based transition detector did not fire; the
    ///     unconditional reconciliation in <c>SyncStateFromStripe</c> now prevents this, and this drift
    ///     check stands as defence-in-depth so any future regression surfaces on the next sync.
    /// </summary>
    ScheduledPriceMissing
}

[PublicAPI]
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum DriftSeverity
{
    Warning,
    Critical
}
