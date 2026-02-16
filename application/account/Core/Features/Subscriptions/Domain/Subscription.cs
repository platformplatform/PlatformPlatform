using System.Collections.Immutable;
using JetBrains.Annotations;
using PlatformPlatform.SharedKernel.Domain;
using PlatformPlatform.SharedKernel.StronglyTypedIds;

namespace PlatformPlatform.Account.Features.Subscriptions.Domain;

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
    }

    public SubscriptionPlan Plan { get; private set; }

    public SubscriptionPlan? ScheduledPlan { get; private set; }

    public StripeCustomerId? StripeCustomerId { get; private set; }

    public StripeSubscriptionId? StripeSubscriptionId { get; private set; }

    public DateTimeOffset? CurrentPeriodEnd { get; private set; }

    public bool CancelAtPeriodEnd { get; private set; }

    public DateTimeOffset? FirstPaymentFailedAt { get; private set; }

    public DateTimeOffset? LastNotificationSentAt { get; private set; }

    public DateTimeOffset? DisputedAt { get; private set; }

    public DateTimeOffset? RefundedAt { get; private set; }

    public CancellationReason? CancellationReason { get; private set; }

    public string? CancellationFeedback { get; private set; }

    public ImmutableArray<PaymentTransaction> PaymentTransactions { get; private set; }

    public PaymentMethod? PaymentMethod { get; private set; }

    public BillingInfo? BillingInfo { get; private set; }

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

    public void SyncFromStripe(
        SubscriptionPlan plan,
        SubscriptionPlan? scheduledPlan,
        StripeSubscriptionId? stripeSubscriptionId,
        DateTimeOffset? currentPeriodEnd,
        bool cancelAtPeriodEnd,
        ImmutableArray<PaymentTransaction> paymentTransactions,
        PaymentMethod? paymentMethod
    )
    {
        Plan = plan;
        ScheduledPlan = scheduledPlan;
        StripeSubscriptionId = stripeSubscriptionId;
        CurrentPeriodEnd = currentPeriodEnd;
        CancelAtPeriodEnd = cancelAtPeriodEnd;
        PaymentTransactions = paymentTransactions;
        PaymentMethod = paymentMethod;
    }

    public void SetPaymentFailed(DateTimeOffset failedAt)
    {
        FirstPaymentFailedAt = failedAt;
    }

    public void SetLastNotificationSentAt(DateTimeOffset sentAt)
    {
        LastNotificationSentAt = sentAt;
    }

    public void ClearPaymentFailure()
    {
        FirstPaymentFailedAt = null;
        LastNotificationSentAt = null;
    }

    public void SetDisputed(DateTimeOffset disputedAt)
    {
        DisputedAt = disputedAt;
    }

    public void ClearDispute()
    {
        DisputedAt = null;
    }

    public void SetRefunded(DateTimeOffset refundedAt)
    {
        RefundedAt = refundedAt;
    }

    public void ClearRefund()
    {
        RefundedAt = null;
    }

    public void ResetToFreePlan()
    {
        Plan = SubscriptionPlan.Basis;
        ScheduledPlan = null;
        StripeSubscriptionId = null;
        CurrentPeriodEnd = null;
        CancelAtPeriodEnd = false;
    }

    public void SetCancellationFeedback(CancellationReason reason, string? feedback)
    {
        CancellationReason = reason;
        CancellationFeedback = feedback;
    }

    public bool HasActiveStripeSubscription()
    {
        return StripeSubscriptionId is not null && Plan != SubscriptionPlan.Basis && !CancelAtPeriodEnd;
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
    string? CreditNoteUrl
);
