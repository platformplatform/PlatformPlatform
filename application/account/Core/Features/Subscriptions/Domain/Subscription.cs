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

    /// <summary>
    ///     The ex-VAT price the subscription will charge after the scheduled downgrade activates at
    ///     <see cref="CurrentPeriodEnd" />. ALWAYS ex-VAT: MRR is revenue accounting, VAT is collected
    ///     on behalf of tax authorities and never our revenue, so every internal recurring-revenue
    ///     number is net-of-tax. Sourced from the price catalog at sync time; the catalog itself
    ///     normalizes from Stripe's inc-VAT listed amount when <c>tax_behavior=inclusive</c>. Null
    ///     when no downgrade is scheduled. The inc-VAT customer-facing amount only appears in
    ///     <see cref="PaymentTransaction" /> for invoice display.
    /// </summary>
    public decimal? ScheduledPriceAmount { get; private set; }

    public StripeCustomerId? StripeCustomerId { get; private set; }

    public StripeSubscriptionId? StripeSubscriptionId { get; private set; }

    /// <summary>
    ///     The ex-VAT price the subscription currently charges per <see cref="CurrentPeriodEnd" />.
    ///     ALWAYS ex-VAT: MRR is revenue accounting, VAT is collected on behalf of tax authorities and
    ///     never our revenue, so every internal recurring-revenue number is net-of-tax. The real Stripe
    ///     client normalizes from <c>price.unit_amount</c> based on <c>price.tax_behavior</c> — for
    ///     <c>inclusive</c> prices it subtracts the VAT component before persisting; for
    ///     <c>exclusive</c> it stores the listed amount unchanged. Null on Basis plans and brand-new
    ///     tenants. The inc-VAT customer-facing amount only appears in <see cref="PaymentTransaction" />
    ///     for invoice display.
    /// </summary>
    public decimal? CurrentPriceAmount { get; private set; }

    public string? CurrentPriceCurrency { get; private set; }

    public DateTimeOffset? CurrentPeriodEnd { get; private set; }

    public bool CancelAtPeriodEnd { get; private set; }

    public DateTimeOffset? FirstPaymentFailedAt { get; private set; }

    public CancellationReason? CancellationReason { get; private set; }

    public string? CancellationFeedback { get; private set; }

    /// <summary>
    ///     Denormalized cache of <c>MIN(occurred_at)</c> across every <c>SubscriptionCreated</c> BillingEvent
    ///     for this tenant. The BillingEvent log is the source of truth; this column exists so paginated reads
    ///     don't have to walk history. Mutated only via <see cref="AdvanceSubscribedSinceBackwardFromBillingEvent" />,
    ///     which is monotonic-backward — a late-arriving recovered event can rewind it earlier, but lifecycle
    ///     transitions (cancel, expire, reactivate on a brand-new Stripe subscription) never move it forward.
    ///     Null when no <c>SubscriptionCreated</c> event has yet been emitted for the tenant.
    /// </summary>
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

    public void SetStripeSubscription(StripeSubscriptionId? stripeSubscriptionId, SubscriptionPlan plan, decimal? currentPriceAmount, string? currentPriceCurrency, DateTimeOffset? currentPeriodEnd, PaymentMethod? paymentMethod)
    {
        StripeSubscriptionId = stripeSubscriptionId;
        Plan = plan;
        CurrentPriceAmount = currentPriceAmount;
        CurrentPriceCurrency = currentPriceCurrency;
        CurrentPeriodEnd = currentPeriodEnd;
        PaymentMethod = paymentMethod;
    }

    /// <summary>
    ///     Denormalized cache of <c>MIN(occurred_at)</c> across every <c>SubscriptionCreated</c> BillingEvent
    ///     for this tenant. Monotonic backward: only assigns when the incoming event is older than the current
    ///     value, so a late-arriving recovered event can rewind the date earlier but lifecycle transitions
    ///     (cancel, expire, reactivate on a brand-new Stripe subscription) never move it forward. Idempotent.
    /// </summary>
    public void AdvanceSubscribedSinceBackwardFromBillingEvent(DateTimeOffset eventOccurredAt)
    {
        if (SubscribedSince is null || eventOccurredAt < SubscribedSince.Value)
        {
            SubscribedSince = eventOccurredAt;
        }
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
    DateTimeOffset? RefundedAt = null,
    // InvoiceTotal is the gross amount Stripe billed for this invoice (= AmountExcludingTax + TaxAmount).
    // Amount is what the customer actually paid from card; AmountFromCredit is the portion absorbed by
    // their Stripe credit balance (e.g. from a prior credit note). The invariant
    // `Amount + AmountFromCredit == InvoiceTotal` lets LTV math count credit-absorbed invoices
    // without conflating them with cash-paid ones. Defaults to 0 so existing JSONB rows backfilled
    // by migration deserialize cleanly.
    decimal InvoiceTotal = 0m,
    decimal AmountFromCredit = 0m
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
    ///     Stripe returned a payment with <c>total_taxes</c> greater than the display amount, which would
    ///     otherwise produce a negative <c>AmountExcludingTax</c>. The value is clamped at zero so the DB
    ///     CHECK does not reject the row (which would 500 the webhook and trigger infinite Stripe retries),
    ///     but the LTV totals silently undercount until the underlying Stripe anomaly is investigated.
    /// </summary>
    AmountExcludingTaxClamped,

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
