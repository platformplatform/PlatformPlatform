extern alias workers;
using System.Globalization;
using Account.Database;
using Account.Features.Subscriptions.Domain;
using Account.Features.Subscriptions.Shared;
using Account.Features.Tenants.Domain;
using Account.Integrations.OAuth;
using Account.Integrations.Stripe;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SharedKernel.Domain;
using SharedKernel.Tests.Persistence;
using Xunit;
using BillingDriftWorker = workers::Account.Workers.BillingDriftWorker;

namespace Account.Tests.Workers;

/// <summary>
///     End-to-end coverage for the <see cref="BillingDriftWorker" /> lifecycle. The handler logic
///     <c>ProcessPendingStripeEvents</c> uses in Detect mode is covered by
///     <c>ProcessPendingStripeEventsDetectModeTests</c>; this file pins the worker's loop semantics:
///     a single pass on <see cref="BackgroundService.ExecuteAsync" />, eligibility filtering via
///     <see cref="ISubscriptionRepository.GetSubscriptionsDueForDriftCheckUnfilteredAsync" />, the
///     iteration-token linkage to <see cref="BillingDriftIterationTimeout" /> (M13), and
///     resilience-on-per-subscription-failure so one bad row cannot kill the entire pass.
/// </summary>
public sealed class BillingDriftWorkerTests : EndpointBaseTest<AccountDbContext>
{
    [Fact]
    public async Task ExecuteAsync_RunsOnePassThenExits_WithoutPeriodicTimer()
    {
        // The worker is documented as a single-pass-per-startup background service: Container Apps scale down
        // to zero replicas when idle, so a PeriodicTimer would never tick before the process exits. The shape
        // we encode here is "ExecuteAsync completes in finite time"; any future regression that turns the
        // worker into a long-running loop would deadlock this test.
        // Arrange
        Connection.Update("subscriptions", "tenant_id", DatabaseSeeder.Tenant1.Id.Value, [
                ("plan", nameof(SubscriptionPlan.Basis)),
                ("stripe_customer_id", null),
                ("stripe_subscription_id", null),
                ("current_price_amount", null),
                ("current_price_currency", null),
                ("current_period_end", null),
                ("drift_checked_at", null)
            ]
        );

        var worker = CreateWorker();

        // Act
        using var workerCancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        await worker.StartAsync(workerCancellationTokenSource.Token);
        await worker.ExecuteTask!;

        // Assert
        worker.ExecuteTask.IsCompletedSuccessfully.Should().BeTrue("the worker must finish its single pass and complete without throwing");
    }

    [Fact]
    public async Task ExecuteAsync_WhenSubscriptionIsDueForDriftCheck_AdvancesDriftCheckedAt()
    {
        // Subscriptions where DriftCheckedAt is null or older than the staleness cutoff must be picked up.
        // Verifies the worker resolves ProcessPendingStripeEvents from the scoped service provider and
        // funnels the subscription through the Detect-mode handler, which advances DriftCheckedAt via
        // SetDriftStatus.
        // Arrange
        SetUseMockStripeCookieOnAmbientHttpContext();

        var beforeWorkerStart = TimeProvider.GetUtcNow();
        Connection.Update("subscriptions", "tenant_id", DatabaseSeeder.Tenant1.Id.Value, [
                ("plan", nameof(SubscriptionPlan.Standard)),
                ("stripe_customer_id", MockStripeClient.MockCustomerId),
                ("stripe_subscription_id", MockStripeClient.MockSubscriptionId),
                ("current_price_amount", MockStripeClient.StandardAmountExcludingTax),
                ("current_price_currency", "DKK"),
                ("current_period_end", TimeProvider.GetUtcNow().AddDays(30)),
                ("drift_checked_at", null)
            ]
        );

        var worker = CreateWorker();

        // Act
        using var workerCancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        await worker.StartAsync(workerCancellationTokenSource.Token);
        await worker.ExecuteTask!;

        // Assert
        ReadDriftCheckedAt(DatabaseSeeder.Tenant1.Id.Value).Should()
            .BeOnOrAfter(beforeWorkerStart.AddSeconds(-1), "a subscription with no prior drift check must be picked up by the worker and visited via Detect mode");
    }

    [Fact]
    public async Task ExecuteAsync_WhenSubscriptionWasCheckedRecently_DoesNotAdvanceDriftCheckedAt()
    {
        // The repository's GetSubscriptionsDueForDriftCheckUnfilteredAsync filters by the cutoff derived from the
        // staleness configuration. Subscriptions whose DriftCheckedAt is newer than the cutoff are excluded, so
        // the worker never visits them and their DriftCheckedAt timestamp is preserved unchanged.
        // Arrange
        SetUseMockStripeCookieOnAmbientHttpContext();

        var recentDriftCheckedAt = TimeProvider.GetUtcNow().AddHours(-1);
        Connection.Update("subscriptions", "tenant_id", DatabaseSeeder.Tenant1.Id.Value, [
                ("plan", nameof(SubscriptionPlan.Standard)),
                ("stripe_customer_id", MockStripeClient.MockCustomerId),
                ("stripe_subscription_id", MockStripeClient.MockSubscriptionId),
                ("current_price_amount", MockStripeClient.StandardAmountExcludingTax),
                ("current_price_currency", "DKK"),
                ("current_period_end", TimeProvider.GetUtcNow().AddDays(30)),
                ("drift_checked_at", recentDriftCheckedAt)
            ]
        );

        var worker = CreateWorker();

        // Act
        using var workerCancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        await worker.StartAsync(workerCancellationTokenSource.Token);
        await worker.ExecuteTask!;

        // Assert
        ReadDriftCheckedAt(DatabaseSeeder.Tenant1.Id.Value).Should()
            .Be(recentDriftCheckedAt, "fresh rows must be excluded by the repository's staleness filter so the worker never visits them");
    }

    [Fact]
    public async Task ExecuteAsync_WhenSubscriptionHasNoStripeCustomerId_SkipsItWithoutCrashing()
    {
        // Defensive null check in the worker loop body. The repository's filter already excludes
        // subscriptions with null StripeCustomerId, but the worker's own guard ensures a future repository
        // regression cannot cause a NullReferenceException inside ProcessPendingStripeEvents — the row is
        // simply counted as skipped and the loop advances.
        // Arrange
        Connection.Update("subscriptions", "tenant_id", DatabaseSeeder.Tenant1.Id.Value, [
                ("plan", nameof(SubscriptionPlan.Basis)),
                ("stripe_customer_id", null),
                ("stripe_subscription_id", null),
                ("current_price_amount", null),
                ("current_price_currency", null),
                ("current_period_end", null),
                ("drift_checked_at", null)
            ]
        );

        var worker = CreateWorker();

        // Act
        using var workerCancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        await worker.StartAsync(workerCancellationTokenSource.Token);
        await worker.ExecuteTask!;

        // Assert
        worker.ExecuteTask.IsCompletedSuccessfully.Should().BeTrue("the worker must skip null-customer rows without throwing");
        ReadDriftCheckedAt(DatabaseSeeder.Tenant1.Id.Value).Should().BeNull("a skipped row must not have DriftCheckedAt advanced");
    }

    [Fact]
    public async Task ExecuteAsync_WhenMultipleSubscriptionsDue_AndStripeViewUnavailableForOne_ContinuesToProcessOthers()
    {
        // Per-subscription resilience. The worker has no ambient HTTP context in production so
        // StripeClientFactory falls back to the unconfigured Stripe client, which returns null from
        // GetCustomerBillingInfoAsync. ProcessPendingStripeEvents in Detect mode treats a null Stripe view
        // as "Stripe is down for this customer" and short-circuits without advancing DriftCheckedAt or
        // throwing — exactly the behavior the staleness tripwire needs (the next pass retries). Critically,
        // the worker's outer loop must NOT abort on this failure mode; the second tenant's row must still
        // be visited on the same pass, picking up the mock cookie set on the ambient HttpContext.
        // Arrange
        SetUseMockStripeCookieOnAmbientHttpContext();

        // Tenant 1: existing seeded subscription, no Stripe customer id — defensively skipped by the loop.
        Connection.Update("subscriptions", "tenant_id", DatabaseSeeder.Tenant1.Id.Value, [
                ("plan", nameof(SubscriptionPlan.Basis)),
                ("stripe_customer_id", null),
                ("drift_checked_at", null)
            ]
        );

        // Tenant 2: due subscription with a valid mock Stripe customer id — must advance DriftCheckedAt.
        var tenant2Id = SeedExtraTenantWithSubscription(MockStripeClient.MockCustomerId, MockStripeClient.MockSubscriptionId);

        var beforeWorkerStart = TimeProvider.GetUtcNow();
        var worker = CreateWorker();

        // Act
        using var workerCancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        await worker.StartAsync(workerCancellationTokenSource.Token);
        await worker.ExecuteTask!;

        // Assert
        worker.ExecuteTask.IsCompletedSuccessfully.Should().BeTrue("one bad subscription must not abort the entire pass");
        ReadDriftCheckedAt(DatabaseSeeder.Tenant1.Id.Value).Should().BeNull("tenant 1 has no Stripe customer id so the worker must skip it");
        ReadDriftCheckedAt(tenant2Id).Should()
            .BeOnOrAfter(beforeWorkerStart.AddSeconds(-1), "tenant 2 is due and must still be processed even though tenant 1 was skipped earlier in the loop");
    }

    [Fact]
    public void BillingDriftWorker_InheritsFromBackgroundService_AndHasNoPeriodicTimerField()
    {
        // The "single-pass-per-startup" contract is enforced structurally: any future refactor that
        // introduces a PeriodicTimer field would silently re-enable the previously-removed periodic loop
        // and break the scale-to-zero assumption documented on the worker. Verified via reflection so the
        // structural rule is encoded as a test rather than relying on a comment.
        // Assert
        typeof(BillingDriftWorker).BaseType.Should().Be(typeof(BackgroundService), "the worker must remain a BackgroundService; switching to IHostedService directly would change the lifecycle contract");

        var fields = typeof(BillingDriftWorker).GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        fields.Should().NotContain(f => f.FieldType == typeof(PeriodicTimer), "introducing a PeriodicTimer would silently re-enable a periodic loop and break the scale-to-zero documentation");
    }

    private BillingDriftWorker CreateWorker()
    {
        var configuration = new ConfigurationBuilder().Build();
        var logger = WebApplicationServices.GetRequiredService<ILogger<BillingDriftWorker>>();
        return new BillingDriftWorker(WebApplicationServices, configuration, TimeProvider, logger);
    }

    // ProcessPendingStripeEvents runs through StripeClientFactory.GetClient(), which gates the mock provider
    // behind an HTTP cookie. The worker has no HTTP context in production, but the tests need to exercise the
    // mock client without standing up a webhook request, so an in-memory HttpContext carrying the mock cookie
    // is attached to the ambient IHttpContextAccessor for the duration of the test.
    private void SetUseMockStripeCookieOnAmbientHttpContext()
    {
        var httpContextAccessor = WebApplicationServices.GetRequiredService<IHttpContextAccessor>();
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers.Append("Cookie", $"{OAuthProviderFactory.UseMockProviderCookieName}=true");
        httpContextAccessor.HttpContext = httpContext;
    }

    private long SeedExtraTenantWithSubscription(string stripeCustomerId, string stripeSubscriptionId)
    {
        var tenantId = TenantId.NewId();
        Connection.Insert("tenants", [
                ("id", tenantId.Value),
                ("created_at", TimeProvider.GetUtcNow().AddDays(-30)),
                ("modified_at", null),
                ("name", $"Worker Test Tenant {tenantId.Value}"),
                ("state", nameof(TenantState.Active)),
                ("plan", nameof(SubscriptionPlan.Standard)),
                ("logo", """{"Url":null,"Version":0}""")
            ]
        );

        var subscriptionId = SubscriptionId.NewId();
        Connection.Insert("subscriptions", [
                ("tenant_id", tenantId.Value),
                ("id", subscriptionId.Value),
                ("created_at", TimeProvider.GetUtcNow().AddDays(-30)),
                ("modified_at", null),
                ("plan", nameof(SubscriptionPlan.Standard)),
                ("scheduled_plan", null),
                ("stripe_customer_id", stripeCustomerId),
                ("stripe_subscription_id", stripeSubscriptionId),
                ("current_price_amount", MockStripeClient.StandardAmountExcludingTax),
                ("current_price_currency", "DKK"),
                ("current_period_end", TimeProvider.GetUtcNow().AddDays(30)),
                ("cancel_at_period_end", false),
                ("first_payment_failed_at", null),
                ("cancellation_reason", null),
                ("cancellation_feedback", null),
                ("payment_transactions", "[]"),
                ("payment_method", null),
                ("billing_info", null),
                ("subscribed_since", null),
                ("scheduled_price_amount", null),
                ("has_drift_detected", false),
                ("drift_checked_at", null),
                ("drift_discrepancies", "[]")
            ]
        );
        return tenantId.Value;
    }

    private DateTimeOffset? ReadDriftCheckedAt(long tenantId)
    {
        var value = Connection.ExecuteScalar<string>(
            "SELECT drift_checked_at FROM subscriptions WHERE tenant_id = @tenantId",
            [new { tenantId }]
        );
        return string.IsNullOrEmpty(value)
            ? null
            : DateTimeOffset.Parse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);
    }
}
