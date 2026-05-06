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
    string Currency,
    PaymentTransactionStatus Status,
    DateTimeOffset Date,
    string? FailureReason,
    string? InvoiceUrl,
    string? CreditNoteUrl,
    SubscriptionPlan? Plan = null
);

[PublicAPI]
public sealed record DriftDiscrepancy(
    DriftDiscrepancyKind Kind,
    string Description,
    DriftSeverity Severity,
    BillingEventType? ExpectedEventType = null,
    string? ExpectedValue = null,
    string? ActualValue = null
);

[PublicAPI]
public enum DriftDiscrepancyKind
{
    MissingEvent,
    ExtraEvent,
    FieldDisagree,
    SubscriptionStateMismatch
}

[PublicAPI]
public enum DriftSeverity
{
    Warning,
    Critical
}
