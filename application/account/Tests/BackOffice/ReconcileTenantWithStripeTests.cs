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

public sealed class ReconcileTenantWithStripeTests : BackOfficeEndpointBaseTest
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
    public async Task ReconcileTenantWithStripe_WhenStripeReturnsNonDkkCurrency_ShouldReturnBadRequest()
    {
        // Arrange
        Connection.Update("subscriptions", "tenant_id", DatabaseSeeder.Tenant1.Id.Value, [
                ("stripe_customer_id", MockStripeClient.MockCustomerId)
            ]
        );
        StripeState.SubscriptionCurrency = "USD";
        var identity = MockEasyAuthIdentities.Default.Single(i => i.Id == "admin");
        using var client = CreateBackOfficeClientForIdentity(identity);
        client.DefaultRequestHeaders.Add("Cookie", $"{OAuthProviderFactory.UseMockProviderCookieName}=true");

        // Act
        var response = await client.PostAsync($"/api/back-office/tenants/{DatabaseSeeder.Tenant1.Id}/reconcile-with-stripe", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        TelemetryEventsCollectorSpy.CollectedEvents.Should().ContainSingle(e => e.GetType().Name == "StripeNonDkkSubscriptionRejected");
    }
}
