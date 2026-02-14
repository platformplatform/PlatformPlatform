using PlatformPlatform.Account.Features.Subscriptions.Domain;

namespace PlatformPlatform.Account.Integrations.Stripe;

public sealed class UnconfiguredStripeClient(ILogger<UnconfiguredStripeClient> logger) : IStripeClient
{
    public Task<string?> CreateCustomerAsync(string tenantName, string email, long tenantId, CancellationToken cancellationToken)
    {
        logger.LogWarning("Stripe is not configured. Cannot create customer for tenant '{TenantName}'", tenantName);
        return Task.FromResult<string?>(null);
    }

    public Task<CheckoutSessionResult?> CreateCheckoutSessionAsync(string stripeCustomerId, SubscriptionPlan plan, string returnUrl, string locale, CancellationToken cancellationToken)
    {
        logger.LogWarning("Stripe is not configured. Cannot create checkout session for customer '{CustomerId}'", stripeCustomerId);
        return Task.FromResult<CheckoutSessionResult?>(null);
    }

    public Task<SubscriptionSyncResult?> SyncSubscriptionStateAsync(string stripeCustomerId, CancellationToken cancellationToken)
    {
        logger.LogWarning("Stripe is not configured. Cannot sync subscription state for customer '{CustomerId}'", stripeCustomerId);
        return Task.FromResult<SubscriptionSyncResult?>(null);
    }

    public Task<string?> GetCheckoutSessionSubscriptionIdAsync(string sessionId, CancellationToken cancellationToken)
    {
        logger.LogWarning("Stripe is not configured. Cannot get checkout session '{SessionId}'", sessionId);
        return Task.FromResult<string?>(null);
    }

    public Task<bool> UpgradeSubscriptionAsync(string stripeSubscriptionId, SubscriptionPlan newPlan, CancellationToken cancellationToken)
    {
        logger.LogWarning("Stripe is not configured. Cannot upgrade subscription '{SubscriptionId}'", stripeSubscriptionId);
        return Task.FromResult(false);
    }

    public Task<bool> ScheduleDowngradeAsync(string stripeSubscriptionId, SubscriptionPlan newPlan, CancellationToken cancellationToken)
    {
        logger.LogWarning("Stripe is not configured. Cannot schedule downgrade for subscription '{SubscriptionId}'", stripeSubscriptionId);
        return Task.FromResult(false);
    }

    public Task<bool> CancelScheduledDowngradeAsync(string stripeSubscriptionId, CancellationToken cancellationToken)
    {
        logger.LogWarning("Stripe is not configured. Cannot cancel scheduled downgrade for subscription '{SubscriptionId}'", stripeSubscriptionId);
        return Task.FromResult(false);
    }

    public Task<bool> CancelSubscriptionAtPeriodEndAsync(string stripeSubscriptionId, CancellationToken cancellationToken)
    {
        logger.LogWarning("Stripe is not configured. Cannot cancel subscription '{SubscriptionId}'", stripeSubscriptionId);
        return Task.FromResult(false);
    }

    public Task<bool> ReactivateSubscriptionAsync(string stripeSubscriptionId, CancellationToken cancellationToken)
    {
        logger.LogWarning("Stripe is not configured. Cannot reactivate subscription '{SubscriptionId}'", stripeSubscriptionId);
        return Task.FromResult(false);
    }

    public StripeHealthResult GetHealth()
    {
        return new StripeHealthResult(
            false,
            false,
            false,
            false,
            false
        );
    }

    public StripeWebhookEventResult? VerifyWebhookSignature(string payload, string signatureHeader)
    {
        logger.LogWarning("Stripe is not configured. Cannot verify webhook signature");
        return null;
    }

    public Task<string?> GetCustomerIdByChargeAsync(string chargeId, CancellationToken cancellationToken)
    {
        logger.LogWarning("Stripe is not configured. Cannot get charge '{ChargeId}'", chargeId);
        return Task.FromResult<string?>(null);
    }

    public Task<string?> GetCustomerIdByInvoiceAsync(string invoiceId, CancellationToken cancellationToken)
    {
        logger.LogWarning("Stripe is not configured. Cannot get invoice '{InvoiceId}'", invoiceId);
        return Task.FromResult<string?>(null);
    }

    public Task<BillingInfo?> GetCustomerBillingInfoAsync(string stripeCustomerId, CancellationToken cancellationToken)
    {
        logger.LogWarning("Stripe is not configured. Cannot get customer billing info for '{CustomerId}'", stripeCustomerId);
        return Task.FromResult<BillingInfo?>(null);
    }

    public Task<bool> UpdateCustomerBillingInfoAsync(string stripeCustomerId, BillingInfo billingInfo, CancellationToken cancellationToken)
    {
        logger.LogWarning("Stripe is not configured. Cannot update billing info for customer '{CustomerId}'", stripeCustomerId);
        return Task.FromResult(false);
    }

    public Task<string?> CreateSetupIntentAsync(string stripeCustomerId, CancellationToken cancellationToken)
    {
        logger.LogWarning("Stripe is not configured. Cannot create SetupIntent for customer '{CustomerId}'", stripeCustomerId);
        return Task.FromResult<string?>(null);
    }

    public Task<string?> GetSetupIntentPaymentMethodAsync(string setupIntentId, CancellationToken cancellationToken)
    {
        logger.LogWarning("Stripe is not configured. Cannot get SetupIntent '{SetupIntentId}'", setupIntentId);
        return Task.FromResult<string?>(null);
    }

    public Task<bool> SetSubscriptionDefaultPaymentMethodAsync(string stripeSubscriptionId, string paymentMethodId, CancellationToken cancellationToken)
    {
        logger.LogWarning("Stripe is not configured. Cannot update payment method for subscription '{SubscriptionId}'", stripeSubscriptionId);
        return Task.FromResult(false);
    }
}
