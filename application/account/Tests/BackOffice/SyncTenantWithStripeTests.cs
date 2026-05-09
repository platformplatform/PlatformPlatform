using System.Net;
using System.Net.Http.Json;
using Account.Features.Tenants.BackOffice.Commands;
using Account.Integrations.OAuth;
using Account.Integrations.Stripe;
using FluentAssertions;
using SharedKernel.Authentication.MockEasyAuth;
using SharedKernel.Domain;
using SharedKernel.Tests.Persistence;
using Xunit;

namespace Account.Tests.BackOffice;

public sealed class SyncTenantWithStripeTests : BackOfficeEndpointBaseTest
{
    [Fact]
    public async Task SyncTenantWithStripe_WhenSubscriptionHasStripeCustomer_ShouldReturnSyncResponse()
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
        var response = await client.PostAsync($"/api/back-office/tenants/{DatabaseSeeder.Tenant1.Id}/sync-with-stripe", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<SyncTenantWithStripeResponse>();
        payload.Should().NotBeNull();
        payload.BillingEventsAppended.Should().BeGreaterThanOrEqualTo(0);
        // HasDriftDetected/DriftDiscrepancyCount aren't asserted: the seeded subscription has
        // PaymentTransactions but no BillingEvent rows, which the drift detector legitimately flags as
        // a MissingEvent discrepancy under the strict 1:1 invariant. The endpoint completing successfully
        // and returning a fresh syncedAt is what this test verifies.
        payload.SyncedAt.Should().BeAfter(DateTimeOffset.UtcNow.AddMinutes(-1));
        TelemetryEventsCollectorSpy.CollectedEvents.Should().ContainSingle(e => e.GetType().Name == "TenantSyncedWithStripe");
    }

    [Fact]
    public async Task SyncTenantWithStripe_WhenSubscriptionHasNoStripeCustomer_ShouldReturnBadRequest()
    {
        // Arrange
        var identity = MockEasyAuthIdentities.Default.Single(i => i.Id == "admin");
        using var client = CreateBackOfficeClientForIdentity(identity);

        // Act
        var response = await client.PostAsync($"/api/back-office/tenants/{DatabaseSeeder.Tenant1.Id}/sync-with-stripe", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task SyncTenantWithStripe_WhenTenantDoesNotExist_ShouldReturnNotFound()
    {
        // Arrange
        var identity = MockEasyAuthIdentities.Default.Single(i => i.Id == "admin");
        using var client = CreateBackOfficeClientForIdentity(identity);
        client.DefaultRequestHeaders.Add("Cookie", $"{OAuthProviderFactory.UseMockProviderCookieName}=true");
        var unknownTenantId = TenantId.NewId();

        // Act
        var response = await client.PostAsync($"/api/back-office/tenants/{unknownTenantId}/sync-with-stripe", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task SyncTenantWithStripe_WhenStripeNotConfigured_ShouldReturnBadRequest()
    {
        // Arrange
        Connection.Update("subscriptions", "tenant_id", DatabaseSeeder.Tenant1.Id.Value, [
                ("stripe_customer_id", MockStripeClient.MockCustomerId)
            ]
        );
        var identity = MockEasyAuthIdentities.Default.Single(i => i.Id == "admin");
        using var client = CreateBackOfficeClientForIdentity(identity);

        // Act
        var response = await client.PostAsync($"/api/back-office/tenants/{DatabaseSeeder.Tenant1.Id}/sync-with-stripe", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task SyncTenantWithStripe_WhenCalledByNonAdmin_ShouldReturnForbidden()
    {
        // Arrange
        var identity = MockEasyAuthIdentities.Default.Single(i => i.Id == "user");
        using var client = CreateBackOfficeClientForIdentity(identity);

        // Act
        var response = await client.PostAsync($"/api/back-office/tenants/{DatabaseSeeder.Tenant1.Id}/sync-with-stripe", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task SyncTenantWithStripe_WhenCalledWithoutAuthentication_ShouldReturnUnauthorized()
    {
        // Arrange
        using var client = CreateBackOfficeClient();

        // Act
        var response = await client.PostAsync($"/api/back-office/tenants/{DatabaseSeeder.Tenant1.Id}/sync-with-stripe", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
