using Account.Features.Subscriptions.Domain;
using Microsoft.Extensions.Configuration;

namespace Account.Integrations.Stripe;

public sealed class MockStripeState
{
    public string? OverrideSubscriptionStatus { get; set; }

    public bool SimulateSubscriptionDeleted { get; set; }

    public bool SimulateCustomerDeleted { get; set; }

    public bool SimulateOpenInvoice { get; set; }

    public DateTimeOffset? CustomerCreated { get; set; }

    // Production only supports DKK (enforced by the StripeClient boundary guard and the DB CHECK
    // constraints on subscriptions.current_price_currency and billing_events.currency). Override
    // on a per-test basis to simulate Stripe returning a non-DKK currency so the boundary guard
    // can be exercised. Default matches production.
    public string SubscriptionCurrency { get; set; } = "DKK";

    // Extra Stripe events the test wants the mock's events.list to return on top of the defaults.
    // Lets a test simulate the events.list view of the world for scenarios where the new
    // events.list-driven emission must see historical events that aren't part of the default mock
    // timeline (e.g. drift detection across earlier customer.subscription.created/deleted pairs).
    public List<StripeReplayEvent> EventsListAdditionalEvents { get; } = [];
}

public sealed class MockStripeClient(IConfiguration configuration, TimeProvider timeProvider, MockStripeState state) : IStripeClient
{
    public const string MockCustomerId = "cus_mock_12345";
    public const string MockSubscriptionId = "sub_mock_12345";
    public const string MockSessionId = "cs_mock_session_12345";
    public const string MockClientSecret = "cs_mock_client_secret_12345";
    public const string MockInvoiceUrl = "https://mock.stripe.local/invoice/12345";
    public const string MockWebhookEventId = "evt_mock_12345";

    public const string MockSubscriptionCreatedEventId = "evt_mock_subscription_created";
    public const string MockPaymentFailedEventId = "evt_mock_payment_failed";
    public const string MockCustomerDeletedEventId = "evt_mock_customer_deleted";

    public const string MockApiVersion = "2025-09-30.preview";

    private readonly bool _isEnabled = configuration.GetValue<bool>("Stripe:AllowMockProvider");

    public Task<StripeCustomerId?> CreateCustomerAsync(string tenantName, string email, long tenantId, CancellationToken cancellationToken)
    {
        EnsureEnabled();
        return Task.FromResult<StripeCustomerId?>(StripeCustomerId.NewId(MockCustomerId));
    }

    public Task<CheckoutSessionResult?> CreateCheckoutSessionAsync(StripeCustomerId stripeCustomerId, SubscriptionPlan plan, string? locale, CancellationToken cancellationToken)
    {
        EnsureEnabled();
        return Task.FromResult<CheckoutSessionResult?>(new CheckoutSessionResult(MockSessionId, MockClientSecret));
    }

    public Task<SubscriptionSyncResult?> SyncSubscriptionStateAsync(StripeCustomerId stripeCustomerId, CancellationToken cancellationToken)
    {
        EnsureEnabled();

        if (state.SimulateSubscriptionDeleted)
        {
            return Task.FromResult<SubscriptionSyncResult?>(null);
        }

        var now = timeProvider.GetUtcNow();
        var transactions = new[]
        {
            new PaymentTransaction(
                PaymentTransactionId.NewId(),
                29.99m,
                23.99m,
                6.00m,
                state.SubscriptionCurrency,
                PaymentTransactionStatus.Succeeded,
                now,
                null,
                MockInvoiceUrl,
                null,
                SubscriptionPlan.Standard
            )
        };

        var result = new SubscriptionSyncResult(
            SubscriptionPlan.Standard,
            null,
            StripeSubscriptionId.NewId(MockSubscriptionId),
            29.99m,
            state.SubscriptionCurrency,
            now.AddDays(30),
            false,
            null,
            null,
            transactions,
            new PaymentMethod("visa", "4242", 12, 2026),
            state.OverrideSubscriptionStatus ?? StripeSubscriptionStatus.Active
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

    public Task<bool> CancelSubscriptionAtPeriodEndAsync(StripeSubscriptionId stripeSubscriptionId, CancellationReason reason, string? feedback, CancellationToken cancellationToken)
    {
        EnsureEnabled();
        return Task.FromResult(true);
    }

    public Task<bool> ReactivateSubscriptionAsync(StripeSubscriptionId stripeSubscriptionId, CancellationToken cancellationToken)
    {
        EnsureEnabled();
        return Task.FromResult(true);
    }

    public Task<PriceCatalogItem[]> GetPriceCatalogAsync(CancellationToken cancellationToken)
    {
        EnsureEnabled();
        return Task.FromResult<PriceCatalogItem[]>([
                new PriceCatalogItem(SubscriptionPlan.Standard, 29.00m, "USD", "month", 1, false),
                new PriceCatalogItem(SubscriptionPlan.Premium, 99.00m, "USD", "month", 1, false)
            ]
        );
    }

    public Task<IReadOnlyDictionary<string, SubscriptionPlan>> GetPlanByPriceIdAsync(CancellationToken cancellationToken)
    {
        EnsureEnabled();
        return Task.FromResult<IReadOnlyDictionary<string, SubscriptionPlan>>(new Dictionary<string, SubscriptionPlan>
            {
                ["price_mock_standard"] = SubscriptionPlan.Standard,
                ["price_mock_premium"] = SubscriptionPlan.Premium
            }
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

        return new StripeWebhookEventResult(eventId, eventType, customerId, MockApiVersion);
    }

    public Task<CustomerBillingResult?> GetCustomerBillingInfoAsync(StripeCustomerId stripeCustomerId, CancellationToken cancellationToken)
    {
        EnsureEnabled();

        if (state.SimulateCustomerDeleted)
        {
            return Task.FromResult<CustomerBillingResult?>(new CustomerBillingResult(null, true));
        }

        var billingInfo = new BillingInfo("Test Organization", new BillingAddress("Vestergade 12", null, "1456", "København K", null, "DK"), "billing@example.com", null);
        var paymentMethod = new PaymentMethod("visa", "4242", 12, 2026);
        var customerCreated = state.CustomerCreated ?? timeProvider.GetUtcNow();
        return Task.FromResult<CustomerBillingResult?>(new CustomerBillingResult(billingInfo, false, paymentMethod, customerCreated));
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

    public Task<bool> SetCustomerDefaultPaymentMethodAsync(StripeCustomerId stripeCustomerId, string paymentMethodId, CancellationToken cancellationToken)
    {
        EnsureEnabled();
        return Task.FromResult(true);
    }

    public Task<OpenInvoiceResult?> GetOpenInvoiceAsync(StripeSubscriptionId stripeSubscriptionId, CancellationToken cancellationToken)
    {
        EnsureEnabled();
        if (state.SimulateOpenInvoice)
        {
            return Task.FromResult<OpenInvoiceResult?>(new OpenInvoiceResult(29.99m, "USD"));
        }

        return Task.FromResult<OpenInvoiceResult?>(null);
    }

    public Task<InvoiceRetryResult?> RetryOpenInvoicePaymentAsync(StripeSubscriptionId stripeSubscriptionId, string? paymentMethodId, CancellationToken cancellationToken)
    {
        EnsureEnabled();
        if (state.SimulateOpenInvoice)
        {
            return Task.FromResult<InvoiceRetryResult?>(new InvoiceRetryResult(true, null));
        }

        return Task.FromResult<InvoiceRetryResult?>(null);
    }

    public Task<UpgradePreviewResult?> GetUpgradePreviewAsync(StripeSubscriptionId stripeSubscriptionId, SubscriptionPlan newPlan, CancellationToken cancellationToken)
    {
        EnsureEnabled();
        var now = timeProvider.GetUtcNow();
        var lineItems = new[]
        {
            new UpgradePreviewLineItem("Unused time on Standard after " + now.ToString("d MMM yyyy"), -14.50m, "USD", true, false),
            new UpgradePreviewLineItem("Remaining time on Premium after " + now.ToString("d MMM yyyy"), 30.00m, "USD", true, false),
            new UpgradePreviewLineItem("Tax", 1.55m, "USD", false, true)
        };
        return Task.FromResult<UpgradePreviewResult?>(new UpgradePreviewResult(17.05m, "USD", lineItems));
    }

    public Task<CheckoutPreviewResult?> GetCheckoutPreviewAsync(StripeCustomerId stripeCustomerId, SubscriptionPlan plan, CancellationToken cancellationToken)
    {
        EnsureEnabled();
        return Task.FromResult<CheckoutPreviewResult?>(new CheckoutPreviewResult(19.00m, "EUR", 0m));
    }

    public Task<SubscribeResult?> CreateSubscriptionWithSavedPaymentMethodAsync(StripeCustomerId stripeCustomerId, SubscriptionPlan plan, CancellationToken cancellationToken)
    {
        EnsureEnabled();
        return Task.FromResult<SubscribeResult?>(new SubscribeResult(null));
    }

    public Task<PaymentTransaction[]?> SyncPaymentTransactionsAsync(StripeCustomerId stripeCustomerId, CancellationToken cancellationToken)
    {
        EnsureEnabled();
        var now = timeProvider.GetUtcNow();
        return Task.FromResult<PaymentTransaction[]?>(
            [
                new PaymentTransaction(PaymentTransactionId.NewId(), 29.99m, 23.99m, 6.00m, state.SubscriptionCurrency, PaymentTransactionStatus.Succeeded, now, null, MockInvoiceUrl, null, SubscriptionPlan.Standard)
            ]
        );
    }

    public Task<StripeReplayEvent[]> GetEventsForCustomerAsync(StripeCustomerId stripeCustomerId, DateTimeOffset? sinceCreated, CancellationToken cancellationToken)
    {
        EnsureEnabled();
        var now = timeProvider.GetUtcNow();
        var events = new List<StripeReplayEvent>
        {
            // Default timeline always starts with a subscription.created event mirroring the mock's
            // SyncSubscriptionStateAsync result (Standard plan on price_mock_standard).
            new(
                MockSubscriptionCreatedEventId,
                "customer.subscription.created",
                now.AddMinutes(-5),
                """{"data":{"object":{"items":{"data":[{"price":{"id":"price_mock_standard"}}]}}}}""",
                MockApiVersion
            )
        };

        if (state.OverrideSubscriptionStatus == StripeSubscriptionStatus.PastDue)
        {
            events.Add(new StripeReplayEvent(
                    MockPaymentFailedEventId,
                    "invoice.payment_failed",
                    now.AddMinutes(-1),
                    """{"data":{"object":{"attempt_count":1,"billing_reason":"subscription_cycle"}}}""",
                    MockApiVersion
                )
            );
        }

        if (state.SimulateCustomerDeleted)
        {
            events.Add(new StripeReplayEvent(MockCustomerDeletedEventId, "customer.deleted", now, "{}", MockApiVersion));
        }

        events.AddRange(state.EventsListAdditionalEvents);

        var filtered = sinceCreated is { } anchor ? events.Where(e => e.CreatedAt >= anchor) : events;
        return Task.FromResult(filtered.OrderBy(e => e.CreatedAt).ThenBy(e => e.EventId).ToArray());
    }

    // ReSharper disable once ReturnTypeCanBeNotNullable
    public string? BuildCustomerDashboardUrl(StripeCustomerId stripeCustomerId)
    {
        EnsureEnabled();
        return $"https://dashboard.stripe.com/test/customers/{stripeCustomerId.Value}";
    }

    private void EnsureEnabled()
    {
        if (!_isEnabled)
        {
            throw new InvalidOperationException("Mock Stripe provider is not enabled.");
        }
    }
}
