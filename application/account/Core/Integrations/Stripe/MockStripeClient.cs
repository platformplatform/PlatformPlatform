using Account.Features.Subscriptions.Domain;
using Microsoft.Extensions.Configuration;
using SharedKernel.Configuration;

namespace Account.Integrations.Stripe;

public sealed class MockStripeState
{
    public string? OverrideSubscriptionStatus { get; set; }

    public bool SimulateSubscriptionDeleted { get; set; }

    public bool SimulateCustomerDeleted { get; set; }

    // Simulates the production behavior of StripeClient.GetCustomerBillingInfoAsync where a
    // StripeException or TaskCanceledException is caught at the integration boundary and the
    // method returns null (per the "never throw from integration clients" project rule).
    public bool SimulateGetCustomerBillingInfoFailure { get; set; }

    public bool SimulateOpenInvoice { get; set; }

    // The platform's architectural promise is that every active Stripe price uses the same currency,
    // derived from Stripe at startup. The mock returns this configured value from every method that
    // surfaces a currency. Override on a per-test basis to simulate Stripe returning a currency that
    // does not match the resolved platform currency so the boundary guard can be exercised.
    public string SubscriptionCurrency { get; set; } = "DKK";

    // Extra Stripe events the test wants the mock's events.list to return on top of the defaults.
    // Lets a test simulate the events.list view of the world for scenarios where the new
    // events.list-driven emission must see historical events that aren't part of the default mock
    // timeline (e.g. drift detection across earlier customer.subscription.created/deleted pairs).
    public List<StripeReplayEvent> EventsListAdditionalEvents { get; } = [];

    // Override the scheduled plan returned by SyncSubscriptionStateAsync. Used to simulate the
    // cancel-then-reschedule edge case where local pre-sync ScheduledPlan equals Stripe post-sync
    // ScheduledPlan and the diff-based transition detector therefore doesn't fire.
    public SubscriptionPlan? ScheduledPlan { get; set; }

    // Plans to omit from the GetPriceCatalogAsync result. Used to simulate the upstream Stripe
    // price-list call returning a partial or empty catalog, so the SingleOrDefault catalog-gap
    // guard in ProcessPendingStripeEvents can be exercised without rolling back the transaction.
    public HashSet<SubscriptionPlan> PriceCatalogOmittedPlans { get; } = [];

    // When set the mock simulates an events.list enumeration that failed partway through. The mock
    // surfaces this through the StripeEventsListResult.Succeeded flag so the anchor-advance guard in
    // ProcessPendingStripeEvents can be exercised end-to-end.
    public bool SimulateEventsListFailure { get; set; }
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

    private readonly bool _isEnabled = ResolveIsEnabled(configuration);

    private static bool ResolveIsEnabled(IConfiguration configuration)
    {
        var allowMockProvider = configuration.GetValue<bool>("Stripe:AllowMockProvider");

        if (allowMockProvider && SharedInfrastructureConfiguration.IsRunningInAzure)
        {
            throw new InvalidOperationException("Mock Stripe provider cannot be enabled in Azure environments.");
        }

        return allowMockProvider;
    }

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
            state.ScheduledPlan,
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
        var catalog = new List<PriceCatalogItem>
        {
            new(SubscriptionPlan.Standard, 29.00m, state.SubscriptionCurrency, "month", 1, false),
            new(SubscriptionPlan.Premium, 99.00m, state.SubscriptionCurrency, "month", 1, false)
        };
        catalog.RemoveAll(item => state.PriceCatalogOmittedPlans.Contains(item.Plan));
        return Task.FromResult(catalog.ToArray());
    }

    public Task<string?> GetPlatformCurrencyAsync(CancellationToken cancellationToken)
    {
        EnsureEnabled();
        return Task.FromResult<string?>(state.SubscriptionCurrency);
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

        return new StripeWebhookEventResult(eventId, eventType, customerId, MockApiVersion, timeProvider.GetUtcNow());
    }

    public Task<CustomerBillingResult?> GetCustomerBillingInfoAsync(StripeCustomerId stripeCustomerId, CancellationToken cancellationToken)
    {
        EnsureEnabled();

        if (state.SimulateGetCustomerBillingInfoFailure)
        {
            return Task.FromResult<CustomerBillingResult?>(null);
        }

        if (state.SimulateCustomerDeleted)
        {
            return Task.FromResult<CustomerBillingResult?>(new CustomerBillingResult(null, true));
        }

        var billingInfo = new BillingInfo("Test Organization", new BillingAddress("Vestergade 12", null, "1456", "København K", null, "DK"), "billing@example.com", null);
        var paymentMethod = new PaymentMethod("visa", "4242", 12, 2026);
        return Task.FromResult<CustomerBillingResult?>(new CustomerBillingResult(billingInfo, false, paymentMethod));
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
            return Task.FromResult<OpenInvoiceResult?>(new OpenInvoiceResult(29.99m, state.SubscriptionCurrency));
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
            new UpgradePreviewLineItem("Unused time on Standard after " + now.ToString("d MMM yyyy"), -14.50m, state.SubscriptionCurrency, true, false),
            new UpgradePreviewLineItem("Remaining time on Premium after " + now.ToString("d MMM yyyy"), 30.00m, state.SubscriptionCurrency, true, false),
            new UpgradePreviewLineItem("Tax", 1.55m, state.SubscriptionCurrency, false, true)
        };
        return Task.FromResult<UpgradePreviewResult?>(new UpgradePreviewResult(17.05m, state.SubscriptionCurrency, lineItems));
    }

    public Task<CheckoutPreviewResult?> GetCheckoutPreviewAsync(StripeCustomerId stripeCustomerId, SubscriptionPlan plan, CancellationToken cancellationToken)
    {
        EnsureEnabled();
        return Task.FromResult<CheckoutPreviewResult?>(new CheckoutPreviewResult(19.00m, state.SubscriptionCurrency, 0m));
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

    public Task<StripeEventsListResult> GetEventsForCustomerAsync(StripeCustomerId stripeCustomerId, DateTimeOffset? sinceCreated, CancellationToken cancellationToken)
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
        var ordered = filtered.OrderBy(e => e.CreatedAt).ThenBy(e => e.EventId).ToArray();
        return Task.FromResult(new StripeEventsListResult(ordered, !state.SimulateEventsListFailure));
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
