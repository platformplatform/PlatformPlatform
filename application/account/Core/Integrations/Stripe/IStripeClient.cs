using PlatformPlatform.Account.Features.Subscriptions.Domain;

namespace PlatformPlatform.Account.Integrations.Stripe;

public interface IStripeClient
{
    Task<string?> CreateCustomerAsync(string tenantName, string email, long tenantId, CancellationToken cancellationToken);

    Task<CheckoutSessionResult?> CreateCheckoutSessionAsync(string stripeCustomerId, SubscriptionPlan plan, string returnUrl, CancellationToken cancellationToken);

    Task<SubscriptionSyncResult?> SyncSubscriptionStateAsync(string stripeCustomerId, CancellationToken cancellationToken);

    Task<string?> GetCheckoutSessionSubscriptionIdAsync(string sessionId, CancellationToken cancellationToken);

    Task<bool> UpgradeSubscriptionAsync(string stripeSubscriptionId, SubscriptionPlan newPlan, CancellationToken cancellationToken);

    Task<bool> ScheduleDowngradeAsync(string stripeSubscriptionId, SubscriptionPlan newPlan, CancellationToken cancellationToken);

    Task<bool> CancelScheduledDowngradeAsync(string stripeSubscriptionId, CancellationToken cancellationToken);

    Task<bool> CancelSubscriptionAtPeriodEndAsync(string stripeSubscriptionId, CancellationToken cancellationToken);

    Task<bool> ReactivateSubscriptionAsync(string stripeSubscriptionId, CancellationToken cancellationToken);

    StripeHealthResult GetHealth();

    StripeWebhookEventResult? VerifyWebhookSignature(string payload, string signatureHeader);

    Task<string?> GetCustomerIdByChargeAsync(string chargeId, CancellationToken cancellationToken);

    Task<BillingInfo?> GetCustomerBillingInfoAsync(string stripeCustomerId, CancellationToken cancellationToken);

    Task<bool> UpdateCustomerBillingInfoAsync(string stripeCustomerId, BillingInfo billingInfo, CancellationToken cancellationToken);

    Task<string?> CreateSetupIntentAsync(string stripeCustomerId, CancellationToken cancellationToken);

    Task<string?> GetSetupIntentPaymentMethodAsync(string setupIntentId, CancellationToken cancellationToken);

    Task<bool> SetSubscriptionDefaultPaymentMethodAsync(string stripeSubscriptionId, string paymentMethodId, CancellationToken cancellationToken);
}

public sealed record StripeWebhookEventResult(string EventId, string EventType, string? CustomerId, string? UnresolvedChargeId, long? MetadataTenantId);

public sealed record CheckoutSessionResult(string SessionId, string ClientSecret);

public sealed record SubscriptionSyncResult(
    SubscriptionPlan Plan,
    SubscriptionPlan? ScheduledPlan,
    string? StripeSubscriptionId,
    DateTimeOffset? CurrentPeriodEnd,
    bool CancelAtPeriodEnd,
    PaymentTransaction[] PaymentTransactions,
    PaymentMethod? PaymentMethod
);

public sealed record StripeHealthResult(bool IsConfigured, bool HasApiKey, bool HasWebhookSecret, bool HasStandardPriceId, bool HasPremiumPriceId);
