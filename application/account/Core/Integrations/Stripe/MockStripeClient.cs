using Microsoft.Extensions.Configuration;
using PlatformPlatform.Account.Features.Subscriptions.Domain;

namespace PlatformPlatform.Account.Integrations.Stripe;

public sealed class MockStripeClient(IConfiguration configuration, TimeProvider timeProvider) : IStripeClient
{
    public const string MockCustomerId = "cus_mock_12345";
    public const string MockSubscriptionId = "sub_mock_12345";
    public const string MockSessionId = "cs_mock_session_12345";
    public const string MockCheckoutUrl = "https://mock.stripe.local/checkout";
    public const string MockBillingPortalUrl = "https://mock.stripe.local/billing-portal";
    public const string MockInvoiceUrl = "https://mock.stripe.local/invoice/12345";
    public const string MockWebhookEventId = "evt_mock_12345";

    private readonly bool _isEnabled = configuration.GetValue<bool>("Stripe:AllowMockProvider");

    public Task<string?> CreateCustomerAsync(string tenantName, string email, CancellationToken cancellationToken)
    {
        EnsureEnabled();
        return Task.FromResult<string?>(MockCustomerId);
    }

    public Task<CheckoutSessionResult?> CreateCheckoutSessionAsync(string stripeCustomerId, SubscriptionPlan plan, string successUrl, string cancelUrl, CancellationToken cancellationToken)
    {
        EnsureEnabled();
        return Task.FromResult<CheckoutSessionResult?>(new CheckoutSessionResult(MockSessionId, MockCheckoutUrl));
    }

    public Task<SubscriptionSyncResult?> SyncSubscriptionStateAsync(string stripeCustomerId, CancellationToken cancellationToken)
    {
        EnsureEnabled();

        var now = timeProvider.GetUtcNow();
        var transactions = new[]
        {
            new PaymentTransaction(
                PaymentTransactionId.NewId(),
                29.99m,
                "usd",
                PaymentTransactionStatus.Succeeded,
                now,
                null,
                MockInvoiceUrl
            )
        };

        var result = new SubscriptionSyncResult(
            SubscriptionPlan.Standard,
            null,
            MockSubscriptionId,
            now.AddDays(30),
            false,
            transactions,
            new PaymentMethod("visa", "4242", 12, 2026)
        );

        return Task.FromResult<SubscriptionSyncResult?>(result);
    }

    public Task<string?> GetCheckoutSessionSubscriptionIdAsync(string sessionId, CancellationToken cancellationToken)
    {
        EnsureEnabled();
        return Task.FromResult<string?>(MockSubscriptionId);
    }

    public Task<bool> UpgradeSubscriptionAsync(string stripeSubscriptionId, SubscriptionPlan newPlan, CancellationToken cancellationToken)
    {
        EnsureEnabled();
        return Task.FromResult(true);
    }

    public Task<bool> ScheduleDowngradeAsync(string stripeSubscriptionId, SubscriptionPlan newPlan, CancellationToken cancellationToken)
    {
        EnsureEnabled();
        return Task.FromResult(true);
    }

    public Task<bool> CancelScheduledDowngradeAsync(string stripeSubscriptionId, CancellationToken cancellationToken)
    {
        EnsureEnabled();
        return Task.FromResult(true);
    }

    public Task<bool> CancelSubscriptionAtPeriodEndAsync(string stripeSubscriptionId, CancellationToken cancellationToken)
    {
        EnsureEnabled();
        return Task.FromResult(true);
    }

    public Task<bool> ReactivateSubscriptionAsync(string stripeSubscriptionId, CancellationToken cancellationToken)
    {
        EnsureEnabled();
        return Task.FromResult(true);
    }

    public Task<string?> CreateBillingPortalSessionAsync(string stripeCustomerId, string returnUrl, string locale, CancellationToken cancellationToken)
    {
        EnsureEnabled();
        return Task.FromResult<string?>(MockBillingPortalUrl);
    }

    public StripeHealthResult GetHealth()
    {
        return new StripeHealthResult(
            _isEnabled,
            _isEnabled,
            _isEnabled,
            _isEnabled,
            _isEnabled
        );
    }

    public StripeWebhookEventResult? VerifyWebhookSignature(string payload, string signatureHeader)
    {
        EnsureEnabled();

        if (signatureHeader == "invalid_signature")
        {
            return null;
        }

        var parts = signatureHeader.Split(',');
        var eventType = "checkout.session.completed";
        var eventId = $"{MockWebhookEventId}_{Guid.NewGuid():N}";

        foreach (var part in parts)
        {
            if (part.StartsWith("event_type:")) eventType = part["event_type:".Length..];
            if (part.StartsWith("event_id:")) eventId = part["event_id:".Length..];
        }

        var customerId = payload.StartsWith("customer:") ? payload.Split(':')[1] : MockCustomerId;

        return new StripeWebhookEventResult(eventId, eventType, customerId);
    }

    private void EnsureEnabled()
    {
        if (!_isEnabled)
        {
            throw new InvalidOperationException("Mock Stripe provider is not enabled.");
        }
    }
}
