using PlatformPlatform.Account.Features.Subscriptions.Domain;

namespace PlatformPlatform.Account.Integrations.Stripe;

public interface IStripeClient
{
    Task<StripeCustomerId?> CreateCustomerAsync(string tenantName, string email, long tenantId, CancellationToken cancellationToken);

    Task<CheckoutSessionResult?> CreateCheckoutSessionAsync(StripeCustomerId stripeCustomerId, SubscriptionPlan plan, string returnUrl, string locale, CancellationToken cancellationToken);

    Task<SubscriptionSyncResult?> SyncSubscriptionStateAsync(StripeCustomerId stripeCustomerId, CancellationToken cancellationToken);

    Task<StripeSubscriptionId?> GetCheckoutSessionSubscriptionIdAsync(string sessionId, CancellationToken cancellationToken);

    Task<bool> UpgradeSubscriptionAsync(StripeSubscriptionId stripeSubscriptionId, SubscriptionPlan newPlan, CancellationToken cancellationToken);

    Task<bool> ScheduleDowngradeAsync(StripeSubscriptionId stripeSubscriptionId, SubscriptionPlan newPlan, CancellationToken cancellationToken);

    Task<bool> CancelScheduledDowngradeAsync(StripeSubscriptionId stripeSubscriptionId, CancellationToken cancellationToken);

    Task<bool> CancelSubscriptionAtPeriodEndAsync(StripeSubscriptionId stripeSubscriptionId, CancellationToken cancellationToken);

    Task<bool> ReactivateSubscriptionAsync(StripeSubscriptionId stripeSubscriptionId, CancellationToken cancellationToken);

    StripeHealthResult GetHealth();

    StripeWebhookEventResult? VerifyWebhookSignature(string payload, string signatureHeader);

    Task<BillingInfo?> GetCustomerBillingInfoAsync(StripeCustomerId stripeCustomerId, CancellationToken cancellationToken);

    Task<bool> UpdateCustomerBillingInfoAsync(StripeCustomerId stripeCustomerId, BillingInfo billingInfo, string locale, CancellationToken cancellationToken);

    Task<string?> CreateSetupIntentAsync(StripeCustomerId stripeCustomerId, CancellationToken cancellationToken);

    Task<string?> GetSetupIntentPaymentMethodAsync(string setupIntentId, CancellationToken cancellationToken);

    Task<bool> SetSubscriptionDefaultPaymentMethodAsync(StripeSubscriptionId stripeSubscriptionId, string paymentMethodId, CancellationToken cancellationToken);
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
    DateTimeOffset? CurrentPeriodEnd,
    bool CancelAtPeriodEnd,
    PaymentTransaction[] PaymentTransactions,
    PaymentMethod? PaymentMethod
);

public sealed record StripeHealthResult(bool IsConfigured, bool HasApiKey, bool HasWebhookSecret, bool HasStandardPriceId, bool HasPremiumPriceId);
