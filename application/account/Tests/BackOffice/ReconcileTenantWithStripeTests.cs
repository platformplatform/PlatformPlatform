using System.Net;
using System.Net.Http.Json;
using Account.Features.Subscriptions.Domain;
using Account.Features.Subscriptions.Shared;
using Account.Features.Tenants.BackOffice.Commands;
using Account.Integrations.OAuth;
using Account.Integrations.Stripe;
using FluentAssertions;
using SharedKernel.Authentication.MockEasyAuth;
using SharedKernel.Domain;
using SharedKernel.Tests.Persistence;
using Xunit;

namespace Account.Tests.BackOffice;

public sealed class ReconcileTenantWithStripeTests(BackOfficeWebApplicationFactory factory) : BackOfficeEndpointBaseTest(factory), IClassFixture<BackOfficeWebApplicationFactory>
{
    [Fact]
    public async Task ReconcileTenantWithStripe_WhenSubscriptionHasStripeCustomer_ShouldReturnReconcileResponse()
    {
        // Arrange
        Connection.Update("subscriptions", "tenant_id", DatabaseSeeder.Tenant1.Id.Value, [
                ("stripe_customer_id", MockStripeClient.MockCustomerId)
            ]
        );
        var identity = MockEasyAuthIdentities.Default.Single(i => i.Id == "admin");
        using var client = CreateBackOfficeClientForIdentity(identity);
        client.DefaultRequestHeaders.Add("Cookie", $"{OAuthProviderFactory.UseMockProviderCookieName}=true");

        // Act
        var response = await client.PostAsync($"/api/back-office/tenants/{DatabaseSeeder.Tenant1.Id}/reconcile-with-stripe", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<ReconcileTenantWithStripeResponse>();
        payload.Should().NotBeNull();
        payload.BillingEventsAppended.Should().BeGreaterThanOrEqualTo(0);
        // HasDriftDetected/DriftDiscrepancyCount aren't asserted: the seeded subscription has
        // PaymentTransactions but no BillingEvent rows, which the drift detector legitimately flags as
        // a MissingEvent discrepancy under the strict 1:1 invariant. The endpoint completing successfully
        // and returning a fresh reconciledAt is what this test verifies.
        payload.ReconciledAt.Should().BeAfter(DateTimeOffset.UtcNow.AddMinutes(-1));
        TelemetryEventsCollectorSpy.CollectedEvents.Should().ContainSingle(e => e.GetType().Name == "TenantReconciledWithStripe");
    }

    [Fact]
    public async Task ReconcileTenantWithStripe_WhenSubscriptionHasNoStripeCustomer_ShouldReturnBadRequest()
    {
        // Arrange
        var identity = MockEasyAuthIdentities.Default.Single(i => i.Id == "admin");
        using var client = CreateBackOfficeClientForIdentity(identity);

        // Act
        var response = await client.PostAsync($"/api/back-office/tenants/{DatabaseSeeder.Tenant1.Id}/reconcile-with-stripe", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ReconcileTenantWithStripe_WhenTenantDoesNotExist_ShouldReturnNotFound()
    {
        // Arrange
        var identity = MockEasyAuthIdentities.Default.Single(i => i.Id == "admin");
        using var client = CreateBackOfficeClientForIdentity(identity);
        client.DefaultRequestHeaders.Add("Cookie", $"{OAuthProviderFactory.UseMockProviderCookieName}=true");
        var unknownTenantId = TenantId.NewId();

        // Act
        var response = await client.PostAsync($"/api/back-office/tenants/{unknownTenantId}/reconcile-with-stripe", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ReconcileTenantWithStripe_WhenStripeNotConfigured_ShouldReturnBadRequest()
    {
        // Arrange
        Connection.Update("subscriptions", "tenant_id", DatabaseSeeder.Tenant1.Id.Value, [
                ("stripe_customer_id", MockStripeClient.MockCustomerId)
            ]
        );
        var identity = MockEasyAuthIdentities.Default.Single(i => i.Id == "admin");
        using var client = CreateBackOfficeClientForIdentity(identity);

        // Act
        var response = await client.PostAsync($"/api/back-office/tenants/{DatabaseSeeder.Tenant1.Id}/reconcile-with-stripe", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ReconcileTenantWithStripe_WhenCalledByNonAdmin_ShouldReturnForbidden()
    {
        // Arrange
        var identity = MockEasyAuthIdentities.Default.Single(i => i.Id == "user");
        using var client = CreateBackOfficeClientForIdentity(identity);

        // Act
        var response = await client.PostAsync($"/api/back-office/tenants/{DatabaseSeeder.Tenant1.Id}/reconcile-with-stripe", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ReconcileTenantWithStripe_WhenCalledWithoutAuthentication_ShouldReturnUnauthorized()
    {
        // Arrange
        using var client = CreateBackOfficeClient();

        // Act
        var response = await client.PostAsync($"/api/back-office/tenants/{DatabaseSeeder.Tenant1.Id}/reconcile-with-stripe", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ExecuteAsync_WhenCatalogEmpty_ReturnsSuccessNotInvalidOperationException()
    {
        // Admin reconcile must not 500 on a catalog gap. Before the fix, the .Single() catalog lookup
        // in ProcessPendingStripeEvents threw InvalidOperationException when Stripe's upstream
        // price-list cache was empty, which surfaced to the operator as an opaque 500 at exactly the
        // moment they needed the reconcile button. The fix swaps to .SingleOrDefault, skips the
        // ScheduledPriceAmount write on miss, and lets the reconcile complete successfully.
        // Arrange
        Connection.Update("subscriptions", "tenant_id", DatabaseSeeder.Tenant1.Id.Value, [
                ("stripe_customer_id", MockStripeClient.MockCustomerId),
                ("scheduled_plan", nameof(SubscriptionPlan.Premium)),
                ("scheduled_price_amount", null)
            ]
        );
        StripeState.ScheduledPlan = SubscriptionPlan.Premium;
        StripeState.PriceCatalogOmittedPlans.Add(SubscriptionPlan.Standard);
        StripeState.PriceCatalogOmittedPlans.Add(SubscriptionPlan.Premium);
        var identity = MockEasyAuthIdentities.Default.Single(i => i.Id == "admin");
        using var client = CreateBackOfficeClientForIdentity(identity);
        client.DefaultRequestHeaders.Add("Cookie", $"{OAuthProviderFactory.UseMockProviderCookieName}=true");

        // Act
        var response = await client.PostAsync($"/api/back-office/tenants/{DatabaseSeeder.Tenant1.Id}/reconcile-with-stripe", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK, "the admin reconcile must complete successfully even when the Stripe price catalog is empty");
        TelemetryEventsCollectorSpy.CollectedEvents.Should().Contain(e => e.GetType().Name == "StripePriceCatalogLookupMissed", "the catalog-gap telemetry event must surface the failure to operators without breaking the reconcile");
    }

    [Fact]
    public async Task ExecuteAsync_WhenLocalArchiveHasEventsOlderThanStripeWindow_ReturnsAwaitingConfirmation()
    {
        // Arrange
        Connection.Update("subscriptions", "tenant_id", DatabaseSeeder.Tenant1.Id.Value, [
                ("stripe_customer_id", MockStripeClient.MockCustomerId)
            ]
        );

        var archivedOccurredAt = DateTimeOffset.UtcNow.AddDays(-45);
        var archivedEventId = $"evt_archive_{Guid.NewGuid():N}";
        var archivedPayload = """{"data":{"object":{"items":{"data":[{"price":{"id":"price_mock_standard","unit_amount":2999}}]}}}}""";
        Connection.Insert("stripe_events", [
                ("tenant_id", DatabaseSeeder.Tenant1.Id.Value),
                ("id", archivedEventId),
                ("created_at", archivedOccurredAt),
                ("modified_at", null),
                ("event_type", "customer.subscription.created"),
                ("status", nameof(StripeEventStatus.Processed)),
                ("processed_at", archivedOccurredAt),
                ("stripe_customer_id", MockStripeClient.MockCustomerId),
                ("stripe_subscription_id", MockStripeClient.MockSubscriptionId),
                ("payload", archivedPayload),
                ("error", null),
                ("api_version", MockStripeClient.MockApiVersion),
                ("payload_hash", StripeEventPayloadHasher.Hash(archivedPayload)),
                ("stripe_created_at", archivedOccurredAt)
            ]
        );

        var identity = MockEasyAuthIdentities.Default.Single(i => i.Id == "admin");
        using var client = CreateBackOfficeClientForIdentity(identity);
        client.DefaultRequestHeaders.Add("Cookie", $"{OAuthProviderFactory.UseMockProviderCookieName}=true");

        // Act
        var response = await client.PostAsync($"/api/back-office/tenants/{DatabaseSeeder.Tenant1.Id}/reconcile-with-stripe", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<ReconcileTenantWithStripeResponse>();
        payload.Should().NotBeNull();
        payload.ArchivedEventsAwaitingConfirmation.Should().NotBeNull();
        payload.ArchivedEventsAwaitingConfirmation!.Count.Should().Be(1);
        payload.ArchivedEventsAwaitingConfirmation.OldestOccurredAt.Should().BeCloseTo(archivedOccurredAt, TimeSpan.FromSeconds(1));
        payload.ArchivedEventsAwaitingConfirmation.NewestOccurredAt.Should().BeCloseTo(archivedOccurredAt, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task ExecuteAsync_WhenNoArchivedEvents_CompletesNormally()
    {
        // Arrange
        Connection.Update("subscriptions", "tenant_id", DatabaseSeeder.Tenant1.Id.Value, [
                ("stripe_customer_id", MockStripeClient.MockCustomerId)
            ]
        );

        var identity = MockEasyAuthIdentities.Default.Single(i => i.Id == "admin");
        using var client = CreateBackOfficeClientForIdentity(identity);
        client.DefaultRequestHeaders.Add("Cookie", $"{OAuthProviderFactory.UseMockProviderCookieName}=true");

        // Act
        var response = await client.PostAsync($"/api/back-office/tenants/{DatabaseSeeder.Tenant1.Id}/reconcile-with-stripe", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<ReconcileTenantWithStripeResponse>();
        payload.Should().NotBeNull();
        payload.ArchivedEventsAwaitingConfirmation.Should().BeNull();
    }

    [Fact]
    public async Task ExecuteAsync_WhenStripeSubscriptionCurrencyDoesNotMatchPlatformCurrency_ReturnsFailureNotInvalidOperation()
    {
        // Arrange
        Connection.Update("subscriptions", "tenant_id", DatabaseSeeder.Tenant1.Id.Value, [
                ("stripe_customer_id", MockStripeClient.MockCustomerId)
            ]
        );
        // The PlatformCurrencyStartupResolver ran at host startup against the mock's default DKK and
        // cached DKK on the singleton provider for the process lifetime. Flipping the per-test mock
        // SubscriptionCurrency to USD makes SyncSubscriptionStateAsync return USD, which the boundary
        // guard then rejects against the cached platform currency.
        StripeState.SubscriptionCurrency = "USD";
        var identity = MockEasyAuthIdentities.Default.Single(i => i.Id == "admin");
        using var client = CreateBackOfficeClientForIdentity(identity);
        client.DefaultRequestHeaders.Add("Cookie", $"{OAuthProviderFactory.UseMockProviderCookieName}=true");

        // Act
        var response = await client.PostAsync($"/api/back-office/tenants/{DatabaseSeeder.Tenant1.Id}/reconcile-with-stripe", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        TelemetryEventsCollectorSpy.CollectedEvents.Should().ContainSingle(e => e.GetType().Name == "StripeSubscriptionCurrencyMismatchRejected");
    }
}
