using System.Globalization;
using Account.Database;
using Account.Features;
using Account.Features.Subscriptions.Domain;
using Account.Features.Subscriptions.Shared;
using Account.Integrations.OAuth;
using Account.Integrations.Stripe;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using SharedKernel.Tests.Persistence;
using Xunit;

namespace Account.Tests.Subscriptions;

/// <summary>
///     Detect mode is the tripwire path consumed by <c>BillingDriftWorker</c>. It reads Stripe state and the
///     local billing-event log, runs the same drift detector as Apply mode, and persists the resulting
///     discrepancies via <see cref="Subscription.SetDriftStatus" /> — but performs no other mutations: no
///     <c>SetStripeSubscription</c>, no <c>SetPaymentTransactions</c>, no <c>AdvanceLastSyncedStripeEventCreatedAt</c>,
///     no <c>billing_events</c> rows appended, and no recovered <c>stripe_events</c> rows inserted.
/// </summary>
public sealed class ProcessPendingStripeEventsDetectModeTests : EndpointBaseTest<AccountDbContext>
{
    [Fact]
    public async Task ProcessPendingStripeEvents_WhenDetectMode_AndSubscriptionDriftsFromStripe_ShouldRecordDriftWithoutMutatingOtherFields()
    {
        // Local subscription is on Premium, but Stripe's mock reports Standard. The drift detector must fire on
        // the Plan mismatch (SubscriptionStateMismatch) and the discrepancy list must be persisted via
        // SetDriftStatus — that must be the ONLY mutation: Plan stays Premium (no SetStripeSubscription in
        // Detect), no billing_events appear, no recovered stripe_events appear, last_synced anchor stays NULL.
        // Arrange
        Connection.Update("subscriptions", "tenant_id", DatabaseSeeder.Tenant1.Id.Value, [
                ("plan", nameof(SubscriptionPlan.Premium)),
                ("stripe_customer_id", MockStripeClient.MockCustomerId),
                ("stripe_subscription_id", MockStripeClient.MockSubscriptionId),
                ("current_price_amount", MockStripeClient.StandardAmountExcludingTax),
                ("current_price_currency", MockStripeClient.MockStandardCurrency),
                ("current_period_end", TimeProvider.GetUtcNow().AddDays(30))
            ]
        );

        var billingEventsBefore = ReadBillingEventCountForTenant1();
        var stripeEventsBefore = ReadStripeEventCount();

        using var scope = WebApplicationServices.CreateScope();
        SetUseMockStripeCookieOnAmbientHttpContext(scope.ServiceProvider);
        var processor = scope.ServiceProvider.GetRequiredService<ProcessPendingStripeEvents>();
        var stripeCustomerId = StripeCustomerId.NewId(MockStripeClient.MockCustomerId);

        // Act
        await processor.ExecuteAsync(stripeCustomerId, true, SyncMode.Detect, CancellationToken.None);

        // Assert
        var driftDiscrepanciesJson = Connection.ExecuteScalar<string>(
            "SELECT drift_discrepancies FROM subscriptions WHERE tenant_id = @tenantId",
            [new { tenantId = DatabaseSeeder.Tenant1.Id.Value }]
        );
        var hasDriftDetected = Connection.ExecuteScalar<long>(
            "SELECT has_drift_detected FROM subscriptions WHERE tenant_id = @tenantId",
            [new { tenantId = DatabaseSeeder.Tenant1.Id.Value }]
        );
        hasDriftDetected.Should().Be(1, $"local Plan=Premium differs from Stripe Plan=Standard so drift must be flagged. Persisted drift_discrepancies = {driftDiscrepanciesJson}");

        driftDiscrepanciesJson.Should().Contain(nameof(DriftDiscrepancyKind.SubscriptionStateMismatch), "the detector must report Plan mismatches");

        ReadDriftCheckedAt().Should().NotBeNull("SetDriftStatus must advance DriftCheckedAt on every detect pass");

        var plan = Connection.ExecuteScalar<string>(
            "SELECT plan FROM subscriptions WHERE tenant_id = @tenantId",
            [new { tenantId = DatabaseSeeder.Tenant1.Id.Value }]
        );
        plan.Should().Be(nameof(SubscriptionPlan.Premium), "Detect mode must not call SetStripeSubscription — Plan must stay at the local pre-detect value");

        var lastSyncedStripeEventCreatedAt = Connection.ExecuteScalar<string>(
            "SELECT last_synced_stripe_event_created_at FROM subscriptions WHERE tenant_id = @tenantId",
            [new { tenantId = DatabaseSeeder.Tenant1.Id.Value }]
        );
        lastSyncedStripeEventCreatedAt.Should().BeNullOrEmpty("Detect mode must not advance the events.list anchor");

        var billingEventsAfter = ReadBillingEventCountForTenant1();
        billingEventsAfter.Should().Be(billingEventsBefore, "Detect mode must not append billing_events rows");

        var stripeEventsAfter = ReadStripeEventCount();
        stripeEventsAfter.Should().Be(stripeEventsBefore, "Detect mode must not insert recovered stripe_events rows");
    }

    [Fact]
    public async Task ProcessPendingStripeEvents_WhenDetectMode_AndSubscriptionInSyncWithStripe_ShouldRecordEmptyDriftAndAdvanceCheckedAt()
    {
        // Local subscription matches Stripe's mock state (Standard / 149.00 DKK ex-VAT) so the detector finds zero
        // discrepancies. SetDriftStatus must still fire so DriftCheckedAt advances — that's how the worker
        // proves it has visited every stale row.
        // Arrange
        Connection.Update("subscriptions", "tenant_id", DatabaseSeeder.Tenant1.Id.Value, [
                ("plan", nameof(SubscriptionPlan.Standard)),
                ("stripe_customer_id", MockStripeClient.MockCustomerId),
                ("stripe_subscription_id", MockStripeClient.MockSubscriptionId),
                ("current_price_amount", MockStripeClient.StandardAmountExcludingTax),
                ("current_price_currency", MockStripeClient.MockStandardCurrency),
                ("current_period_end", TimeProvider.GetUtcNow().AddDays(30))
            ]
        );

        var billingEventsBefore = ReadBillingEventCountForTenant1();
        var stripeEventsBefore = ReadStripeEventCount();
        var before = TimeProvider.GetUtcNow();

        using var scope = WebApplicationServices.CreateScope();
        SetUseMockStripeCookieOnAmbientHttpContext(scope.ServiceProvider);
        var processor = scope.ServiceProvider.GetRequiredService<ProcessPendingStripeEvents>();
        var stripeCustomerId = StripeCustomerId.NewId(MockStripeClient.MockCustomerId);

        // Act
        await processor.ExecuteAsync(stripeCustomerId, true, SyncMode.Detect, CancellationToken.None);

        // Assert
        var hasDriftDetected = Connection.ExecuteScalar<long>(
            "SELECT has_drift_detected FROM subscriptions WHERE tenant_id = @tenantId",
            [new { tenantId = DatabaseSeeder.Tenant1.Id.Value }]
        );
        hasDriftDetected.Should().Be(0, "the local subscription matches Stripe so no discrepancies should be recorded");

        ReadDriftCheckedAt().Should().BeOnOrAfter(before.AddSeconds(-1), "SetDriftStatus must advance DriftCheckedAt even when the discrepancy list is empty");

        var billingEventsAfter = ReadBillingEventCountForTenant1();
        billingEventsAfter.Should().Be(billingEventsBefore, "Detect mode must not append billing_events rows on the happy path");

        var stripeEventsAfter = ReadStripeEventCount();
        stripeEventsAfter.Should().Be(stripeEventsBefore, "Detect mode must not insert recovered stripe_events rows on the happy path");
    }

    [Fact]
    public async Task ExecuteAsync_WhenStripeViewUnavailable_DoesNotAdvanceDriftCheckedAt()
    {
        // Production StripeClient.GetCustomerBillingInfoAsync catches StripeException/TaskCanceledException
        // at the integration boundary and returns null (per the integration-client contract). When Detect mode
        // observes a null Stripe view it must NOT advance DriftCheckedAt — otherwise the row looks fresh for
        // 23h and the BillingDriftWorker tripwire silently skips it on the next pass while Stripe is down.
        // Arrange
        var existingDriftCheckedAt = TimeProvider.GetUtcNow().AddHours(-25);
        Connection.Update("subscriptions", "tenant_id", DatabaseSeeder.Tenant1.Id.Value, [
                ("plan", nameof(SubscriptionPlan.Standard)),
                ("stripe_customer_id", MockStripeClient.MockCustomerId),
                ("stripe_subscription_id", MockStripeClient.MockSubscriptionId),
                ("current_price_amount", MockStripeClient.StandardAmountExcludingTax),
                ("current_price_currency", MockStripeClient.MockStandardCurrency),
                ("current_period_end", TimeProvider.GetUtcNow().AddDays(30)),
                ("drift_checked_at", existingDriftCheckedAt)
            ]
        );

        StripeState.SimulateGetCustomerBillingInfoFailure = true;
        TelemetryEventsCollectorSpy.Reset();

        using var scope = WebApplicationServices.CreateScope();
        SetUseMockStripeCookieOnAmbientHttpContext(scope.ServiceProvider);
        var processor = scope.ServiceProvider.GetRequiredService<ProcessPendingStripeEvents>();
        var stripeCustomerId = StripeCustomerId.NewId(MockStripeClient.MockCustomerId);

        // Act
        await processor.ExecuteAsync(stripeCustomerId, true, SyncMode.Detect, CancellationToken.None);

        // Assert
        ReadDriftCheckedAt().Should().Be(existingDriftCheckedAt, "Detect mode must leave DriftCheckedAt unchanged when the Stripe view is unavailable so the next pass retries");
        TelemetryEventsCollectorSpy.CollectedEvents.Should().ContainSingle(e => e.GetType().Name == nameof(BillingDriftSkippedDueToStripeUnavailable));
    }

    [Fact]
    public async Task ExecuteAsync_WhenStripeViewAvailable_AdvancesDriftCheckedAtAsBefore()
    {
        // Regression guard for the happy path: the Stripe view is available, drift detection runs, and
        // DriftCheckedAt advances to the current clock.
        // Arrange
        var existingDriftCheckedAt = TimeProvider.GetUtcNow().AddHours(-25);
        Connection.Update("subscriptions", "tenant_id", DatabaseSeeder.Tenant1.Id.Value, [
                ("plan", nameof(SubscriptionPlan.Standard)),
                ("stripe_customer_id", MockStripeClient.MockCustomerId),
                ("stripe_subscription_id", MockStripeClient.MockSubscriptionId),
                ("current_price_amount", MockStripeClient.StandardAmountExcludingTax),
                ("current_price_currency", MockStripeClient.MockStandardCurrency),
                ("current_period_end", TimeProvider.GetUtcNow().AddDays(30)),
                ("drift_checked_at", existingDriftCheckedAt)
            ]
        );

        var before = TimeProvider.GetUtcNow();

        using var scope = WebApplicationServices.CreateScope();
        SetUseMockStripeCookieOnAmbientHttpContext(scope.ServiceProvider);
        var processor = scope.ServiceProvider.GetRequiredService<ProcessPendingStripeEvents>();
        var stripeCustomerId = StripeCustomerId.NewId(MockStripeClient.MockCustomerId);

        // Act
        await processor.ExecuteAsync(stripeCustomerId, true, SyncMode.Detect, CancellationToken.None);

        // Assert
        ReadDriftCheckedAt().Should().BeOnOrAfter(before.AddSeconds(-1), "Detect mode must advance DriftCheckedAt on the happy path so the worker can prove it has visited the row");
    }

    [Fact]
    public async Task ExecuteAsync_AdvancesDriftCheckedAtWithoutLockingRow()
    {
        // Detect mode is the BillingDriftWorker tripwire. It must read the subscription without acquiring a
        // FOR UPDATE row lock — otherwise the worker holds the lock for the duration of the Stripe roundtrip
        // and blocks the webhook hot path on every subscription it iterates over. The targeted column-only
        // UPDATE for drift status must still land: drift_checked_at advances, drift_discrepancies persists,
        // and no other column on the row is rewritten.
        // Arrange
        var existingDriftCheckedAt = TimeProvider.GetUtcNow().AddHours(-25);
        var existingCurrentPeriodEnd = TimeProvider.GetUtcNow().AddDays(30);
        Connection.Update("subscriptions", "tenant_id", DatabaseSeeder.Tenant1.Id.Value, [
                ("plan", nameof(SubscriptionPlan.Premium)),
                ("stripe_customer_id", MockStripeClient.MockCustomerId),
                ("stripe_subscription_id", MockStripeClient.MockSubscriptionId),
                ("current_price_amount", 99.99m),
                ("current_price_currency", MockStripeClient.MockStandardCurrency),
                ("current_period_end", existingCurrentPeriodEnd),
                ("drift_checked_at", existingDriftCheckedAt)
            ]
        );

        var before = TimeProvider.GetUtcNow();

        using var scope = WebApplicationServices.CreateScope();
        SetUseMockStripeCookieOnAmbientHttpContext(scope.ServiceProvider);
        var processor = scope.ServiceProvider.GetRequiredService<ProcessPendingStripeEvents>();
        var stripeCustomerId = StripeCustomerId.NewId(MockStripeClient.MockCustomerId);

        // Act
        await processor.ExecuteAsync(stripeCustomerId, true, SyncMode.Detect, CancellationToken.None);

        // Assert
        ReadDriftCheckedAt().Should().BeOnOrAfter(before.AddSeconds(-1), "the targeted column-only update must advance DriftCheckedAt even though the read did not acquire a row lock");

        var hasDriftDetected = Connection.ExecuteScalar<long>(
            "SELECT has_drift_detected FROM subscriptions WHERE tenant_id = @tenantId",
            [new { tenantId = DatabaseSeeder.Tenant1.Id.Value }]
        );
        hasDriftDetected.Should().Be(1, "local Plan=Premium differs from Stripe Plan=Standard so the targeted UPDATE must persist the discrepancy list");

        var plan = Connection.ExecuteScalar<string>(
            "SELECT plan FROM subscriptions WHERE tenant_id = @tenantId",
            [new { tenantId = DatabaseSeeder.Tenant1.Id.Value }]
        );
        plan.Should().Be(nameof(SubscriptionPlan.Premium), "the targeted column-only UPDATE must rewrite only the drift status fields — Plan must keep the local pre-detect value");

        var currentPriceAmount = Connection.ExecuteScalar<string>(
            "SELECT current_price_amount FROM subscriptions WHERE tenant_id = @tenantId",
            [new { tenantId = DatabaseSeeder.Tenant1.Id.Value }]
        );
        decimal.Parse(currentPriceAmount, CultureInfo.InvariantCulture).Should().Be(99.99m, "the targeted column-only UPDATE must not rewrite CurrentPriceAmount");
    }

    // ProcessPendingStripeEvents runs through StripeClientFactory.GetClient(), which gates the mock provider
    // behind an HTTP cookie. The detect-mode worker has no HTTP context in production, but the tests need to
    // exercise the mock client without standing up a webhook request — so an in-memory HttpContext carrying
    // the mock cookie is attached to the ambient IHttpContextAccessor for the duration of the test.
    private static void SetUseMockStripeCookieOnAmbientHttpContext(IServiceProvider serviceProvider)
    {
        var httpContextAccessor = serviceProvider.GetRequiredService<IHttpContextAccessor>();
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers.Append("Cookie", $"{OAuthProviderFactory.UseMockProviderCookieName}=true");
        httpContextAccessor.HttpContext = httpContext;
    }

    private long ReadBillingEventCountForTenant1()
    {
        return Connection.ExecuteScalar<long>(
            "SELECT COUNT(*) FROM billing_events WHERE tenant_id = @tenantId",
            [new { tenantId = DatabaseSeeder.Tenant1.Id.Value }]
        );
    }

    private long ReadStripeEventCount()
    {
        return Connection.ExecuteScalar<long>("SELECT COUNT(*) FROM stripe_events", []);
    }

    private DateTimeOffset? ReadDriftCheckedAt()
    {
        var value = Connection.ExecuteScalar<string>(
            "SELECT drift_checked_at FROM subscriptions WHERE tenant_id = @tenantId",
            [new { tenantId = DatabaseSeeder.Tenant1.Id.Value }]
        );
        return string.IsNullOrEmpty(value)
            ? null
            : DateTimeOffset.Parse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);
    }
}
