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

    // Per-test override for the currency the mock emits from SyncSubscriptionStateAsync (and the rest
    // of the methods that surface a currency). Left null so the mock's PlatformCurrency tracks the
    // resolver-populated platform currency by default; set to a different ISO 4217 code in a test to
    // simulate Stripe returning a currency that does not match the cached platform currency so the
    // boundary guard can be exercised. Setting to MockStripeClient.MockStandardCurrency explicitly is
    // equivalent to leaving it null when the resolver also resolves the default.
    public string? SubscriptionCurrency { get; set; }

    // Optional reference to the singleton populated by PlatformCurrencyStartupResolver. When set, the
    // mock emits this currency on every method so the mock and the production resolver agree on the
    // platform currency without each test having to wire SubscriptionCurrency explicitly. Left null in
    // the UnconfiguredStripeClient path and in unit tests that construct MockStripeState directly.
    public IPlatformCurrencyProvider? PlatformCurrencyProvider { get; init; }

    // Resolves the currency the mock emits at request time. The per-test override wins so the mismatch
    // guard can be exercised; otherwise the resolver-populated currency wins; otherwise fall back to
    // the constant default so unit tests that construct MockStripeState directly don't observe null.
    public string PlatformCurrency => SubscriptionCurrency ?? PlatformCurrencyProvider?.Currency ?? MockStripeClient.MockStandardCurrency;

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
    public const string MockPaymentMethodId = "pm_mock_12345";
    public const string MockInvoiceId = "in_mock_12345";
    public const string MockWebhookEventId = "evt_mock_12345";

    public const string MockSubscriptionCreatedEventId = "evt_mock_subscription_created";
    public const string MockPaymentMethodAttachedEventId = "evt_mock_payment_method_attached";
    public const string MockInvoicePaymentSucceededEventId = "evt_mock_invoice_payment_succeeded";
    public const string MockPaymentFailedEventId = "evt_mock_payment_failed";
    public const string MockCustomerDeletedEventId = "evt_mock_customer_deleted";

    public const string MockApiVersion = "2025-09-30.preview";

    // Default mock currency used both as MockStripeState.SubscriptionCurrency's seed value and as a
    // searchable test constant. Tests reference MockStripeClient.MockStandardCurrency instead of a raw
    // "DKK" literal so flipping the seed flips the entire mock and test surface in lock-step.
    public const string MockStandardCurrency = "DKK";

    // Mock plan amounts follow the platform's ex-VAT convention for internal recurring-revenue numbers.
    // MRR is revenue accounting; VAT is collected on behalf of tax authorities and is never our revenue,
    // so CurrentPriceAmount, ScheduledPriceAmount, and every BillingEvent amount column are ALWAYS ex-VAT.
    // PaymentTransaction is the one exception — it exposes the inc-VAT customer-facing display amount as
    // Amount, plus AmountExcludingTax and TaxAmount for the invoice breakdown. The mock derives the tax
    // and inc-VAT amounts at request time from the active platform currency via VatRatesByCurrency, so
    // running the developer Stripe sandbox in any of the supported currencies stays internally consistent.
    public const decimal StandardAmountExcludingTax = 149.00m;
    public const decimal PremiumAmountExcludingTax = 299.00m;

    // Per-currency VAT rates the mock applies when synthesising inc-VAT and tax amounts. Real-world
    // VAT rates depend on merchant country plus customer country (B2B vs B2C), so production must read
    // them off the Stripe invoice. For the mock these defaults are sufficient to keep the local-dev
    // experience self-consistent across the four currencies developer sandboxes are likely to be in.
    // EU rates vary across member states (17%-27%); 21% is the rough EU average and is approximate.
    // TODO: configurable VAT rate per merchant/country once the platform onboards multiple jurisdictions.
    private static readonly Dictionary<string, decimal> VatRatesByCurrency = new(StringComparer.OrdinalIgnoreCase)
    {
        ["DKK"] = 0.25m,
        ["EUR"] = 0.21m,
        ["USD"] = 0m,
        ["GBP"] = 0.20m
    };

    private readonly bool _isEnabled = ResolveIsEnabled(configuration);

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
        var currency = state.PlatformCurrency;
        var (standardIncludingTax, standardTaxAmount) = ComputeAmountBreakdown(StandardAmountExcludingTax, currency);
        var transactions = new[]
        {
            new PaymentTransaction(
                PaymentTransactionId.NewId(),
                standardIncludingTax,
                StandardAmountExcludingTax,
                standardTaxAmount,
                currency,
                PaymentTransactionStatus.Succeeded,
                now,
                null,
                MockInvoiceUrl,
                null,
                SubscriptionPlan.Standard,
                null,
                standardIncludingTax
            )
        };

        var result = new SubscriptionSyncResult(
            SubscriptionPlan.Standard,
            state.ScheduledPlan,
            StripeSubscriptionId.NewId(MockSubscriptionId),
            StandardAmountExcludingTax,
            currency,
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
        var currency = state.PlatformCurrency;
        var catalog = new List<PriceCatalogItem>
        {
            new(SubscriptionPlan.Standard, StandardAmountExcludingTax, currency, "month", 1, false),
            new(SubscriptionPlan.Premium, PremiumAmountExcludingTax, currency, "month", 1, false)
        };
        catalog.RemoveAll(item => state.PriceCatalogOmittedPlans.Contains(item.Plan));
        return Task.FromResult(catalog.ToArray());
    }

    public Task<string?> GetPlatformCurrencyAsync(CancellationToken cancellationToken)
    {
        EnsureEnabled();
        // PlatformCurrencyStartupResolver calls this method exactly once at host startup to seed the
        // platform-currency singleton. The per-test SubscriptionCurrency override is not the right
        // source here because it represents Stripe drift from the seed; the resolver wants the seed
        // itself. Fall through to PlatformCurrency so the same property the rest of the mock reads
        // is also what the resolver caches — a non-null override therefore *does* flow through to the
        // resolver when wired before host startup (this is what PlatformCurrencyProviderTests exercises).
        return Task.FromResult<string?>(state.PlatformCurrency);
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
            var currency = state.PlatformCurrency;
            var (standardIncludingTax, _) = ComputeAmountBreakdown(StandardAmountExcludingTax, currency);
            return Task.FromResult<OpenInvoiceResult?>(new OpenInvoiceResult(standardIncludingTax, currency));
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
        var currency = state.PlatformCurrency;
        var lineItems = new[]
        {
            new UpgradePreviewLineItem("Unused time on Standard after " + now.ToString("d MMM yyyy"), -14.50m, currency, true, false),
            new UpgradePreviewLineItem("Remaining time on Premium after " + now.ToString("d MMM yyyy"), 30.00m, currency, true, false),
            new UpgradePreviewLineItem("Tax", 1.55m, currency, false, true)
        };
        return Task.FromResult<UpgradePreviewResult?>(new UpgradePreviewResult(17.05m, currency, lineItems));
    }

    public Task<CheckoutPreviewResult?> GetCheckoutPreviewAsync(StripeCustomerId stripeCustomerId, SubscriptionPlan plan, CancellationToken cancellationToken)
    {
        EnsureEnabled();
        return Task.FromResult<CheckoutPreviewResult?>(new CheckoutPreviewResult(19.00m, state.PlatformCurrency, 0m));
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
        var currency = state.PlatformCurrency;
        var (standardIncludingTax, standardTaxAmount) = ComputeAmountBreakdown(StandardAmountExcludingTax, currency);
        return Task.FromResult<PaymentTransaction[]?>(
            [
                new PaymentTransaction(PaymentTransactionId.NewId(), standardIncludingTax, StandardAmountExcludingTax, standardTaxAmount, currency, PaymentTransactionStatus.Succeeded, now, null, MockInvoiceUrl, null, SubscriptionPlan.Standard, null, standardIncludingTax)
            ]
        );
    }

    public Task<StripeEventsListResult> GetEventsForCustomerAsync(StripeCustomerId stripeCustomerId, DateTimeOffset? sinceCreated, CancellationToken cancellationToken)
    {
        EnsureEnabled();
        var now = timeProvider.GetUtcNow();
        // Stripe encodes invoice amounts in the smallest currency unit (øre for DKK, cents for USD/EUR).
        // The amount_paid value is inc-VAT, amount_excluding_tax is ex-VAT, and tax is the difference —
        // mirrors the PaymentTransaction triple the mock produces elsewhere so the replayer sees an
        // internally consistent timeline. The currency code is the lowercase ISO 4217 platform currency.
        // See https://docs.stripe.com/currencies.
        var currency = state.PlatformCurrency;
        var (standardIncludingTax, standardTaxAmount) = ComputeAmountBreakdown(StandardAmountExcludingTax, currency);
        var amountPaidMinorUnits = (long)(standardIncludingTax * 100m);
        var amountExcludingTaxMinorUnits = (long)(StandardAmountExcludingTax * 100m);
        var taxMinorUnits = (long)(standardTaxAmount * 100m);
        var currencyCodeForPayload = currency.ToLowerInvariant();
        var paymentMethodAttachedPayload = "{\"data\":{\"object\":{\"id\":\"" + MockPaymentMethodId + "\",\"type\":\"card\",\"customer\":\"" + MockCustomerId + "\"}}}";
        var invoicePaymentSucceededPayload = "{\"data\":{\"object\":{\"id\":\"" + MockInvoiceId + "\",\"amount_paid\":" + amountPaidMinorUnits + ",\"amount_excluding_tax\":" + amountExcludingTaxMinorUnits + ",\"tax\":" + taxMinorUnits + ",\"currency\":\"" + currencyCodeForPayload + "\",\"subscription\":\"" + MockSubscriptionId + "\",\"status\":\"paid\",\"billing_reason\":\"subscription_create\"}}}";
        var events = new List<StripeReplayEvent>
        {
            // The default timeline mirrors the state SyncSubscriptionStateAsync returns: an attached
            // payment method, an active Standard subscription on price_mock_standard, and a paid invoice
            // for the first billing cycle. The replayer must consume all three so the BillingEvent
            // timeline matches the live subscription state without surfacing spurious drift.
            new(
                MockPaymentMethodAttachedEventId,
                "payment_method.attached",
                now.AddMinutes(-6),
                paymentMethodAttachedPayload,
                MockApiVersion
            ),
            new(
                MockSubscriptionCreatedEventId,
                "customer.subscription.created",
                now.AddMinutes(-5),
                """{"data":{"object":{"items":{"data":[{"price":{"id":"price_mock_standard"}}]}}}}""",
                MockApiVersion
            ),
            new(
                MockInvoicePaymentSucceededEventId,
                "invoice.payment_succeeded",
                now.AddMinutes(-4),
                invoicePaymentSucceededPayload,
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

    public string? BuildCustomerDashboardUrl(StripeCustomerId stripeCustomerId)
    {
        EnsureEnabled();
        // Mock customers don't exist in Stripe's Dashboard, so returning null tells the back-office UI
        // to hide the "Open in Stripe" menu item entirely — clicking the link would otherwise land on a
        // 404 inside Stripe and confuse operators investigating a mock-mode tenant.
        return null;
    }

    private static (decimal AmountIncludingTax, decimal TaxAmount) ComputeAmountBreakdown(decimal amountExcludingTax, string currency)
    {
        // Unknown currencies fall back to 0% so the mock stays self-consistent in a developer sandbox
        // configured for a currency outside the registry — the only observable effect is that inc-VAT
        // equals ex-VAT and tax is zero, which production reconciliation handles via the same path as
        // a B2B reverse-charge invoice with no tax line.
        var vatRate = VatRatesByCurrency.GetValueOrDefault(currency, 0m);
        var taxAmount = Math.Round(amountExcludingTax * vatRate, 2);
        return (amountExcludingTax + taxAmount, taxAmount);
    }

    private static bool ResolveIsEnabled(IConfiguration configuration)
    {
        var allowMockProvider = configuration.GetValue<bool>("Stripe:AllowMockProvider");

        if (allowMockProvider && SharedInfrastructureConfiguration.IsRunningInAzure)
        {
            throw new InvalidOperationException("Mock Stripe provider cannot be enabled in Azure environments.");
        }

        return allowMockProvider;
    }

    private void EnsureEnabled()
    {
        if (!_isEnabled)
        {
            throw new InvalidOperationException("Mock Stripe provider is not enabled.");
        }
    }
}
