using PlatformPlatform.Account.Features.Subscriptions.Domain;

namespace PlatformPlatform.Account.Integrations.Stripe;

public interface IStripeClient
{
    Task<string?> CreateCustomerAsync(string tenantName, string email, CancellationToken cancellationToken);

    Task<CheckoutSessionResult?> CreateCheckoutSessionAsync(string stripeCustomerId, SubscriptionPlan plan, string successUrl, string cancelUrl, CancellationToken cancellationToken);

    Task<SubscriptionSyncResult?> SyncSubscriptionStateAsync(string stripeCustomerId, CancellationToken cancellationToken);

    Task<string?> GetCheckoutSessionSubscriptionIdAsync(string sessionId, CancellationToken cancellationToken);

    Task<bool> UpgradeSubscriptionAsync(string stripeSubscriptionId, SubscriptionPlan newPlan, CancellationToken cancellationToken);

    Task<bool> ScheduleDowngradeAsync(string stripeSubscriptionId, SubscriptionPlan newPlan, CancellationToken cancellationToken);

    Task<bool> CancelSubscriptionAtPeriodEndAsync(string stripeSubscriptionId, CancellationToken cancellationToken);

    Task<bool> ReactivateSubscriptionAsync(string stripeSubscriptionId, CancellationToken cancellationToken);

    Task<string?> CreateBillingPortalSessionAsync(string stripeCustomerId, string returnUrl, string locale, CancellationToken cancellationToken);

    StripeHealthResult GetHealth();
}

public sealed record CheckoutSessionResult(string SessionId, string Url);

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
