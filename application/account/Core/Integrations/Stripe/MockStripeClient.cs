using Microsoft.Extensions.Configuration;
using PlatformPlatform.Account.Features.Subscriptions.Domain;

namespace PlatformPlatform.Account.Integrations.Stripe;

public sealed class MockStripeClient(IConfiguration configuration, TimeProvider timeProvider) : IStripeClient
{
    public const string MockCustomerId = "cus_mock_12345";
    public const string MockSubscriptionId = "sub_mock_12345";
    public const string MockSessionId = "cs_mock_session_12345";
    public const string MockClientSecret = "cs_mock_client_secret_12345";
    public const string MockInvoiceUrl = "https://mock.stripe.local/invoice/12345";
    public const string MockWebhookEventId = "evt_mock_12345";

    private readonly bool _isEnabled = configuration.GetValue<bool>("Stripe:AllowMockProvider");

    public Task<StripeCustomerId?> CreateCustomerAsync(string tenantName, string email, long tenantId, CancellationToken cancellationToken)
    {
        EnsureEnabled();
        return Task.FromResult<StripeCustomerId?>(StripeCustomerId.NewId(MockCustomerId));
    }

    public Task<CheckoutSessionResult?> CreateCheckoutSessionAsync(StripeCustomerId stripeCustomerId, SubscriptionPlan plan, string returnUrl, string? locale, CancellationToken cancellationToken)
    {
        EnsureEnabled();
        return Task.FromResult<CheckoutSessionResult?>(new CheckoutSessionResult(MockSessionId, MockClientSecret));
    }

    public Task<SubscriptionSyncResult?> SyncSubscriptionStateAsync(StripeCustomerId stripeCustomerId, CancellationToken cancellationToken)
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
                MockInvoiceUrl,
                null
            )
        };

        var result = new SubscriptionSyncResult(
            SubscriptionPlan.Standard,
            null,
            StripeSubscriptionId.NewId(MockSubscriptionId),
            now.AddDays(30),
            false,
            transactions,
            new PaymentMethod("visa", "4242", 12, 2026)
        );

        return Task.FromResult<SubscriptionSyncResult?>(result);
    }

    public Task<StripeSubscriptionId?> GetCheckoutSessionSubscriptionIdAsync(string sessionId, CancellationToken cancellationToken)
    {
        EnsureEnabled();
        return Task.FromResult<StripeSubscriptionId?>(StripeSubscriptionId.NewId(MockSubscriptionId));
    }

    public Task<UpgradeSubscriptionResult?> UpgradeSubscriptionAsync(StripeSubscriptionId stripeSubscriptionId, SubscriptionPlan newPlan, CancellationToken cancellationToken)
    {
        EnsureEnabled();
        return Task.FromResult<UpgradeSubscriptionResult?>(new UpgradeSubscriptionResult(null));
    }

    public Task<bool> ScheduleDowngradeAsync(StripeSubscriptionId stripeSubscriptionId, SubscriptionPlan newPlan, CancellationToken cancellationToken)
    {
        EnsureEnabled();
        return Task.FromResult(true);
    }

    public Task<bool> CancelScheduledDowngradeAsync(StripeSubscriptionId stripeSubscriptionId, CancellationToken cancellationToken)
    {
        EnsureEnabled();
        return Task.FromResult(true);
    }

    public Task<bool> CancelSubscriptionAtPeriodEndAsync(StripeSubscriptionId stripeSubscriptionId, CancellationToken cancellationToken)
    {
        EnsureEnabled();
        return Task.FromResult(true);
    }

    public Task<bool> ReactivateSubscriptionAsync(StripeSubscriptionId stripeSubscriptionId, CancellationToken cancellationToken)
    {
        EnsureEnabled();
        return Task.FromResult(true);
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

        var customerIdString = payload.StartsWith("customer:") ? payload.Split(':')[1] : payload == "no_customer" ? null : MockCustomerId;
        StripeCustomerId.TryParse(customerIdString, out var customerId);

        return new StripeWebhookEventResult(eventId, eventType, customerId);
    }

    public Task<BillingInfo?> GetCustomerBillingInfoAsync(StripeCustomerId stripeCustomerId, CancellationToken cancellationToken)
    {
        EnsureEnabled();
        var billingInfo = new BillingInfo("Test Organization", new BillingAddress("Vestergade 12", null, "1456", "K\u00f8benhavn K", null, "DK"), "billing@example.com", null);
        return Task.FromResult<BillingInfo?>(billingInfo);
    }

    public Task<bool> UpdateCustomerBillingInfoAsync(StripeCustomerId stripeCustomerId, BillingInfo billingInfo, string? locale, CancellationToken cancellationToken)
    {
        EnsureEnabled();
        return Task.FromResult(true);
    }

    public Task<bool> SyncCustomerTaxIdAsync(StripeCustomerId stripeCustomerId, string? taxId, CancellationToken cancellationToken)
    {
        EnsureEnabled();
        if (taxId == "INVALID")
        {
            return Task.FromResult(false);
        }

        return Task.FromResult(true);
    }

    public Task<string?> CreateSetupIntentAsync(StripeCustomerId stripeCustomerId, CancellationToken cancellationToken)
    {
        EnsureEnabled();
        return Task.FromResult<string?>("seti_mock_client_secret_12345");
    }

    public Task<string?> GetSetupIntentPaymentMethodAsync(string setupIntentId, CancellationToken cancellationToken)
    {
        EnsureEnabled();
        return Task.FromResult<string?>("pm_mock_12345");
    }

    public Task<bool> SetSubscriptionDefaultPaymentMethodAsync(StripeSubscriptionId stripeSubscriptionId, string paymentMethodId, CancellationToken cancellationToken)
    {
        EnsureEnabled();
        return Task.FromResult(true);
    }

    public Task<bool?> RetryOpenInvoicePaymentAsync(StripeSubscriptionId stripeSubscriptionId, string paymentMethodId, CancellationToken cancellationToken)
    {
        EnsureEnabled();
        return Task.FromResult<bool?>(null);
    }

    public Task<UpgradePreviewResult?> GetUpgradePreviewAsync(StripeSubscriptionId stripeSubscriptionId, SubscriptionPlan newPlan, CancellationToken cancellationToken)
    {
        EnsureEnabled();
        var now = timeProvider.GetUtcNow();
        var lineItems = new[]
        {
            new UpgradePreviewLineItem("Unused time on Standard after " + now.ToString("d MMM yyyy"), -14.50m, "usd", true),
            new UpgradePreviewLineItem("Remaining time on Premium after " + now.ToString("d MMM yyyy"), 30.00m, "usd", true),
            new UpgradePreviewLineItem("Tax", 1.55m, "usd", false)
        };
        return Task.FromResult<UpgradePreviewResult?>(new UpgradePreviewResult(17.05m, "usd", lineItems));
    }

    public Task<CheckoutPreviewResult?> GetCheckoutPreviewAsync(StripeCustomerId stripeCustomerId, SubscriptionPlan plan, CancellationToken cancellationToken)
    {
        EnsureEnabled();
        return Task.FromResult<CheckoutPreviewResult?>(new CheckoutPreviewResult(19.00m, "eur", 0m));
    }

    private void EnsureEnabled()
    {
        if (!_isEnabled)
        {
            throw new InvalidOperationException("Mock Stripe provider is not enabled.");
        }
    }
}
