using PlatformPlatform.Account.Features.Subscriptions.Domain;

namespace PlatformPlatform.Account.Integrations.Stripe;

public sealed class UnconfiguredStripeClient(ILogger<UnconfiguredStripeClient> logger) : IStripeClient
{
    public Task<StripeCustomerId?> CreateCustomerAsync(string tenantName, string email, long tenantId, CancellationToken cancellationToken)
    {
        logger.LogWarning("Stripe is not configured. Cannot create customer for tenant '{TenantName}'", tenantName);
        return Task.FromResult<StripeCustomerId?>(null);
    }

    public Task<CheckoutSessionResult?> CreateCheckoutSessionAsync(StripeCustomerId stripeCustomerId, SubscriptionPlan plan, string returnUrl, string? locale, CancellationToken cancellationToken)
    {
        logger.LogWarning("Stripe is not configured. Cannot create checkout session for customer '{CustomerId}'", stripeCustomerId);
        return Task.FromResult<CheckoutSessionResult?>(null);
    }

    public Task<SubscriptionSyncResult?> SyncSubscriptionStateAsync(StripeCustomerId stripeCustomerId, CancellationToken cancellationToken)
    {
        logger.LogWarning("Stripe is not configured. Cannot sync subscription state for customer '{CustomerId}'", stripeCustomerId);
        return Task.FromResult<SubscriptionSyncResult?>(null);
    }

    public Task<StripeSubscriptionId?> GetCheckoutSessionSubscriptionIdAsync(string sessionId, CancellationToken cancellationToken)
    {
        logger.LogWarning("Stripe is not configured. Cannot get checkout session '{SessionId}'", sessionId);
        return Task.FromResult<StripeSubscriptionId?>(null);
    }

    public Task<UpgradeSubscriptionResult?> UpgradeSubscriptionAsync(StripeSubscriptionId stripeSubscriptionId, SubscriptionPlan newPlan, CancellationToken cancellationToken)
    {
        logger.LogWarning("Stripe is not configured. Cannot upgrade subscription '{SubscriptionId}'", stripeSubscriptionId);
        return Task.FromResult<UpgradeSubscriptionResult?>(null);
    }

    public Task<bool> ScheduleDowngradeAsync(StripeSubscriptionId stripeSubscriptionId, SubscriptionPlan newPlan, CancellationToken cancellationToken)
    {
        logger.LogWarning("Stripe is not configured. Cannot schedule downgrade for subscription '{SubscriptionId}'", stripeSubscriptionId);
        return Task.FromResult(false);
    }

    public Task<bool> CancelScheduledDowngradeAsync(StripeSubscriptionId stripeSubscriptionId, CancellationToken cancellationToken)
    {
        logger.LogWarning("Stripe is not configured. Cannot cancel scheduled downgrade for subscription '{SubscriptionId}'", stripeSubscriptionId);
        return Task.FromResult(false);
    }

    public Task<bool> CancelSubscriptionAtPeriodEndAsync(StripeSubscriptionId stripeSubscriptionId, CancellationReason reason, string? feedback, CancellationToken cancellationToken)
    {
        logger.LogWarning("Stripe is not configured. Cannot cancel subscription '{SubscriptionId}'", stripeSubscriptionId);
        return Task.FromResult(false);
    }

    public Task<bool> ReactivateSubscriptionAsync(StripeSubscriptionId stripeSubscriptionId, CancellationToken cancellationToken)
    {
        logger.LogWarning("Stripe is not configured. Cannot reactivate subscription '{SubscriptionId}'", stripeSubscriptionId);
        return Task.FromResult(false);
    }

    public Task<string?> GetPriceIdAsync(SubscriptionPlan plan, CancellationToken cancellationToken)
    {
        logger.LogWarning("Stripe is not configured. Cannot resolve price for plan '{Plan}'", plan);
        return Task.FromResult<string?>(null);
    }

    public Task<PriceCatalogItem[]> GetPriceCatalogAsync(CancellationToken cancellationToken)
    {
        logger.LogWarning("Stripe is not configured. Cannot get pricing catalog");
        return Task.FromResult<PriceCatalogItem[]>([]);
    }

    public StripeWebhookEventResult? VerifyWebhookSignature(string payload, string signatureHeader)
    {
        logger.LogWarning("Stripe is not configured. Cannot verify webhook signature");
        return null;
    }

    public Task<CustomerBillingResult?> GetCustomerBillingInfoAsync(StripeCustomerId stripeCustomerId, CancellationToken cancellationToken)
    {
        logger.LogWarning("Stripe is not configured. Cannot get customer billing info for '{CustomerId}'", stripeCustomerId);
        return Task.FromResult<CustomerBillingResult?>(null);
    }

    public Task<bool> UpdateCustomerBillingInfoAsync(StripeCustomerId stripeCustomerId, BillingInfo billingInfo, string? locale, CancellationToken cancellationToken)
    {
        logger.LogWarning("Stripe is not configured. Cannot update billing info for customer '{CustomerId}'", stripeCustomerId);
        return Task.FromResult(false);
    }

    public Task<bool> SyncCustomerTaxIdAsync(StripeCustomerId stripeCustomerId, string? taxId, CancellationToken cancellationToken)
    {
        logger.LogWarning("Stripe is not configured. Cannot sync tax ID for customer '{CustomerId}'", stripeCustomerId);
        return Task.FromResult(false);
    }

    public Task<string?> CreateSetupIntentAsync(StripeCustomerId stripeCustomerId, CancellationToken cancellationToken)
    {
        logger.LogWarning("Stripe is not configured. Cannot create SetupIntent for customer '{CustomerId}'", stripeCustomerId);
        return Task.FromResult<string?>(null);
    }

    public Task<string?> GetSetupIntentPaymentMethodAsync(string setupIntentId, CancellationToken cancellationToken)
    {
        logger.LogWarning("Stripe is not configured. Cannot get SetupIntent '{SetupIntentId}'", setupIntentId);
        return Task.FromResult<string?>(null);
    }

    public Task<bool> SetSubscriptionDefaultPaymentMethodAsync(StripeSubscriptionId stripeSubscriptionId, string paymentMethodId, CancellationToken cancellationToken)
    {
        logger.LogWarning("Stripe is not configured. Cannot update payment method for subscription '{SubscriptionId}'", stripeSubscriptionId);
        return Task.FromResult(false);
    }

    public Task<bool?> RetryOpenInvoicePaymentAsync(StripeSubscriptionId stripeSubscriptionId, string paymentMethodId, CancellationToken cancellationToken)
    {
        logger.LogWarning("Stripe is not configured. Cannot retry invoice payment for subscription '{SubscriptionId}'", stripeSubscriptionId);
        return Task.FromResult<bool?>(false);
    }

    public Task<UpgradePreviewResult?> GetUpgradePreviewAsync(StripeSubscriptionId stripeSubscriptionId, SubscriptionPlan newPlan, CancellationToken cancellationToken)
    {
        logger.LogWarning("Stripe is not configured. Cannot get upgrade preview for subscription '{SubscriptionId}'", stripeSubscriptionId);
        return Task.FromResult<UpgradePreviewResult?>(null);
    }

    public Task<CheckoutPreviewResult?> GetCheckoutPreviewAsync(StripeCustomerId stripeCustomerId, SubscriptionPlan plan, CancellationToken cancellationToken)
    {
        logger.LogWarning("Stripe is not configured. Cannot get checkout preview for customer '{CustomerId}'", stripeCustomerId);
        return Task.FromResult<CheckoutPreviewResult?>(null);
    }

    public Task<PaymentTransaction[]?> SyncPaymentTransactionsAsync(StripeCustomerId stripeCustomerId, CancellationToken cancellationToken)
    {
        logger.LogWarning("Stripe is not configured. Cannot sync payment transactions for customer '{CustomerId}'", stripeCustomerId);
        return Task.FromResult<PaymentTransaction[]?>(null);
    }
}
