using Account.Features.Subscriptions.Domain;

namespace Account.Integrations.Stripe;

public interface IStripeClient
{
    Task<StripeCustomerId?> CreateCustomerAsync(string tenantName, string email, long tenantId, CancellationToken cancellationToken);

    Task<CheckoutSessionResult?> CreateCheckoutSessionAsync(StripeCustomerId stripeCustomerId, SubscriptionPlan plan, string? locale, CancellationToken cancellationToken);

    Task<SubscriptionSyncResult?> SyncSubscriptionStateAsync(StripeCustomerId stripeCustomerId, CancellationToken cancellationToken);

    Task<StripeSubscriptionId?> GetCheckoutSessionSubscriptionIdAsync(string sessionId, CancellationToken cancellationToken);

    Task<UpgradeSubscriptionResult?> UpgradeSubscriptionAsync(StripeSubscriptionId stripeSubscriptionId, SubscriptionPlan newPlan, CancellationToken cancellationToken);

    Task<bool> ScheduleDowngradeAsync(StripeSubscriptionId stripeSubscriptionId, SubscriptionPlan newPlan, CancellationToken cancellationToken);

    Task<bool> CancelScheduledDowngradeAsync(StripeSubscriptionId stripeSubscriptionId, CancellationToken cancellationToken);

    Task<bool> CancelSubscriptionAtPeriodEndAsync(StripeSubscriptionId stripeSubscriptionId, CancellationReason reason, string? feedback, CancellationToken cancellationToken);

    Task<bool> ReactivateSubscriptionAsync(StripeSubscriptionId stripeSubscriptionId, CancellationToken cancellationToken);

    Task<PriceCatalogItem[]> GetPriceCatalogAsync(CancellationToken cancellationToken);

    Task<IReadOnlyDictionary<string, SubscriptionPlan>> GetPlanByPriceIdAsync(CancellationToken cancellationToken);

    /// <summary>
    ///     Returns the single currency observed across active Stripe prices. The application's
    ///     architectural promise is that every active Stripe price uses the same currency; an
    ///     implementation must throw when it observes more than one distinct currency. Returns
    ///     <c>null</c> from <see cref="UnconfiguredStripeClient" /> so callers can detect the
    ///     unconfigured environment without exception handling.
    /// </summary>
    Task<string?> GetPlatformCurrencyAsync(CancellationToken cancellationToken);

    StripeWebhookEventResult? VerifyWebhookSignature(string payload, string signatureHeader);

    Task<CustomerBillingResult?> GetCustomerBillingInfoAsync(StripeCustomerId stripeCustomerId, CancellationToken cancellationToken);

    Task<bool> UpdateCustomerBillingInfoAsync(StripeCustomerId stripeCustomerId, BillingInfo billingInfo, string? locale, CancellationToken cancellationToken);

    Task<bool> SyncCustomerTaxIdAsync(StripeCustomerId stripeCustomerId, string? taxId, CancellationToken cancellationToken);

    Task<string?> CreateSetupIntentAsync(StripeCustomerId stripeCustomerId, CancellationToken cancellationToken);

    Task<string?> GetSetupIntentPaymentMethodAsync(string setupIntentId, CancellationToken cancellationToken);

    Task<bool> SetSubscriptionDefaultPaymentMethodAsync(StripeSubscriptionId stripeSubscriptionId, string paymentMethodId, CancellationToken cancellationToken);

    Task<bool> SetCustomerDefaultPaymentMethodAsync(StripeCustomerId stripeCustomerId, string paymentMethodId, CancellationToken cancellationToken);

    Task<OpenInvoiceResult?> GetOpenInvoiceAsync(StripeSubscriptionId stripeSubscriptionId, CancellationToken cancellationToken);

    Task<InvoiceRetryResult?> RetryOpenInvoicePaymentAsync(StripeSubscriptionId stripeSubscriptionId, string? paymentMethodId, CancellationToken cancellationToken);

    Task<UpgradePreviewResult?> GetUpgradePreviewAsync(StripeSubscriptionId stripeSubscriptionId, SubscriptionPlan newPlan, CancellationToken cancellationToken);

    Task<CheckoutPreviewResult?> GetCheckoutPreviewAsync(StripeCustomerId stripeCustomerId, SubscriptionPlan plan, CancellationToken cancellationToken);

    Task<SubscribeResult?> CreateSubscriptionWithSavedPaymentMethodAsync(StripeCustomerId stripeCustomerId, SubscriptionPlan plan, CancellationToken cancellationToken);

    Task<PaymentTransaction[]?> SyncPaymentTransactionsAsync(StripeCustomerId stripeCustomerId, CancellationToken cancellationToken);

    /// <summary>
    ///     Returns Stripe events related to a customer (last 30 days — see
    ///     https://docs.stripe.com/api/events) via the events.list API. This is the authoritative
    ///     source for the hot path: every webhook-driven sync calls this with
    ///     <paramref name="sinceCreated" /> set to the subscription's last-synced anchor so the
    ///     BillingEvent ledger is rebuilt from Stripe's view of the world, never from
    ///     <c>stripe_events.payload</c>. The local archive is a cold backup read only by the
    ///     admin reconcile command for events older than the 30-day window.
    /// </summary>
    Task<StripeReplayEvent[]> GetEventsForCustomerAsync(StripeCustomerId stripeCustomerId, DateTimeOffset? sinceCreated, CancellationToken cancellationToken);

    /// <summary>
    ///     Builds the Stripe Dashboard URL for a customer. Returns null when no Stripe API key is
    ///     configured. The URL points at the test-mode dashboard for `sk_test_*` keys and the live
    ///     dashboard otherwise — matching how the Stripe Dashboard itself disambiguates modes.
    /// </summary>
    string? BuildCustomerDashboardUrl(StripeCustomerId stripeCustomerId);
}

public sealed record StripeReplayEvent(string EventId, string EventType, DateTimeOffset CreatedAt, string Payload, string ApiVersion);

public sealed record StripeWebhookEventResult(
    string EventId,
    string EventType,
    StripeCustomerId? CustomerId,
    string ApiVersion,
    DateTimeOffset Created
);

public sealed record CheckoutSessionResult(string SessionId, string ClientSecret);

public sealed record SubscriptionSyncResult(
    SubscriptionPlan Plan,
    SubscriptionPlan? ScheduledPlan,
    StripeSubscriptionId? StripeSubscriptionId,
    decimal? CurrentPriceAmount,
    string? CurrentPriceCurrency,
    DateTimeOffset? CurrentPeriodEnd,
    bool CancelAtPeriodEnd,
    CancellationReason? CancellationReason,
    string? CancellationFeedback,
    PaymentTransaction[]? PaymentTransactions,
    PaymentMethod? PaymentMethod,
    string? SubscriptionStatus
);

public sealed record CustomerBillingResult(BillingInfo? BillingInfo, bool IsCustomerDeleted, PaymentMethod? PaymentMethod = null);

public sealed record OpenInvoiceResult(decimal AmountDue, string Currency);

public sealed record InvoiceRetryResult(bool Paid, string? ClientSecret, string? ErrorMessage = null);

public sealed record UpgradeSubscriptionResult(string? ClientSecret, string? ErrorMessage = null);

public sealed record SubscribeResult(string? ClientSecret);

public sealed record UpgradePreviewResult(decimal TotalAmount, string Currency, UpgradePreviewLineItem[] LineItems);

public sealed record UpgradePreviewLineItem(string Description, decimal Amount, string Currency, bool IsProration, bool IsTax);

public sealed record CheckoutPreviewResult(decimal TotalAmount, string Currency, decimal TaxAmount);

public sealed record PriceCatalogItem(
    SubscriptionPlan Plan,
    decimal UnitAmount,
    string Currency,
    string Interval,
    int IntervalCount,
    bool TaxInclusive
);

public static class StripeSubscriptionStatus
{
    public const string Active = "active";
    public const string Incomplete = "incomplete";
    public const string PastDue = "past_due";
}
