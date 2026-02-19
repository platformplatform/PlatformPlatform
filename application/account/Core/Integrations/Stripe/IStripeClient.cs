using PlatformPlatform.Account.Features.Subscriptions.Domain;

namespace PlatformPlatform.Account.Integrations.Stripe;

public interface IStripeClient
{
    Task<StripeCustomerId?> CreateCustomerAsync(string tenantName, string email, long tenantId, CancellationToken cancellationToken);

    Task<CheckoutSessionResult?> CreateCheckoutSessionAsync(StripeCustomerId stripeCustomerId, SubscriptionPlan plan, string returnUrl, string? locale, CancellationToken cancellationToken);

    Task<SubscriptionSyncResult?> SyncSubscriptionStateAsync(StripeCustomerId stripeCustomerId, CancellationToken cancellationToken);

    Task<StripeSubscriptionId?> GetCheckoutSessionSubscriptionIdAsync(string sessionId, CancellationToken cancellationToken);

    Task<UpgradeSubscriptionResult?> UpgradeSubscriptionAsync(StripeSubscriptionId stripeSubscriptionId, SubscriptionPlan newPlan, CancellationToken cancellationToken);

    Task<bool> ScheduleDowngradeAsync(StripeSubscriptionId stripeSubscriptionId, SubscriptionPlan newPlan, CancellationToken cancellationToken);

    Task<bool> CancelScheduledDowngradeAsync(StripeSubscriptionId stripeSubscriptionId, CancellationToken cancellationToken);

    Task<bool> CancelSubscriptionAtPeriodEndAsync(StripeSubscriptionId stripeSubscriptionId, CancellationReason reason, string? feedback, CancellationToken cancellationToken);

    Task<bool> ReactivateSubscriptionAsync(StripeSubscriptionId stripeSubscriptionId, CancellationToken cancellationToken);

    Task<string?> GetPriceIdAsync(SubscriptionPlan plan, CancellationToken cancellationToken);

    Task<PriceCatalogItem[]> GetPriceCatalogAsync(CancellationToken cancellationToken);

    StripeWebhookEventResult? VerifyWebhookSignature(string payload, string signatureHeader);

    Task<CustomerBillingResult?> GetCustomerBillingInfoAsync(StripeCustomerId stripeCustomerId, CancellationToken cancellationToken);

    Task<bool> UpdateCustomerBillingInfoAsync(StripeCustomerId stripeCustomerId, BillingInfo billingInfo, string? locale, CancellationToken cancellationToken);

    Task<bool> SyncCustomerTaxIdAsync(StripeCustomerId stripeCustomerId, string? taxId, CancellationToken cancellationToken);

    Task<string?> CreateSetupIntentAsync(StripeCustomerId stripeCustomerId, CancellationToken cancellationToken);

    Task<string?> GetSetupIntentPaymentMethodAsync(string setupIntentId, CancellationToken cancellationToken);

    Task<bool> SetSubscriptionDefaultPaymentMethodAsync(StripeSubscriptionId stripeSubscriptionId, string paymentMethodId, CancellationToken cancellationToken);

    Task<bool?> RetryOpenInvoicePaymentAsync(StripeSubscriptionId stripeSubscriptionId, string paymentMethodId, CancellationToken cancellationToken);

    Task<UpgradePreviewResult?> GetUpgradePreviewAsync(StripeSubscriptionId stripeSubscriptionId, SubscriptionPlan newPlan, CancellationToken cancellationToken);

    Task<CheckoutPreviewResult?> GetCheckoutPreviewAsync(StripeCustomerId stripeCustomerId, SubscriptionPlan plan, CancellationToken cancellationToken);
}

public sealed record StripeWebhookEventResult(
    string EventId,
    string EventType,
    StripeCustomerId? CustomerId
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
    PaymentTransaction[] PaymentTransactions,
    PaymentMethod? PaymentMethod,
    string? SubscriptionStatus
);

public sealed record CustomerBillingResult(BillingInfo? BillingInfo, bool IsCustomerDeleted);

public sealed record UpgradeSubscriptionResult(string? ClientSecret);

public sealed record UpgradePreviewResult(decimal TotalAmount, string Currency, UpgradePreviewLineItem[] LineItems);

public sealed record UpgradePreviewLineItem(string Description, decimal Amount, string Currency, bool IsProration);

public sealed record CheckoutPreviewResult(decimal TotalAmount, string Currency, decimal TaxAmount);

public sealed record PriceCatalogItem(SubscriptionPlan Plan, decimal UnitAmount, string Currency, string FormattedPrice);

public static class StripeSubscriptionStatus
{
    public const string Active = "active";
    public const string PastDue = "past_due";
}
