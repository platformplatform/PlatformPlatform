using System.Net;
using System.Text.Json;
using Account.Features.Subscriptions.Domain;
using FluentAssertions;
using SharedKernel.Authentication.MockEasyAuth;
using SharedKernel.Domain;
using SharedKernel.Tests.Persistence;
using Xunit;

namespace Account.Tests.BackOffice;

public sealed class AcknowledgeBillingDriftTests : BackOfficeEndpointBaseTest
{
    [Fact]
    public async Task AcknowledgeBillingDrift_WhenSubscriptionHasDrift_ShouldClearDrift()
    {
        // Arrange
        var discrepancies = new[]
        {
            new DriftDiscrepancy(DriftDiscrepancyKind.MissingEvent, "Missing billing event for payment.", DriftSeverity.Warning)
        };
        Connection.Update("subscriptions", "tenant_id", DatabaseSeeder.Tenant1.Id.Value, [
                ("has_drift_detected", true),
                ("drift_checked_at", DateTimeOffset.UtcNow.AddMinutes(-5)),
                ("drift_discrepancies", JsonSerializer.Serialize(discrepancies))
            ]
        );
        var identity = MockEasyAuthIdentities.Default.Single(i => i.Id == "admin");
        using var client = CreateBackOfficeClientForIdentity(identity);

        // Act
        var response = await client.PostAsync($"/api/back-office/tenants/{DatabaseSeeder.Tenant1.Id}/drift/acknowledge", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var hasDriftDetected = Connection.ExecuteScalar<long>(
            "SELECT has_drift_detected FROM subscriptions WHERE tenant_id = @tenantId", [new { tenantId = DatabaseSeeder.Tenant1.Id.Value }]
        );
        hasDriftDetected.Should().Be(0);
        TelemetryEventsCollectorSpy.CollectedEvents.Should().ContainSingle(e => e.GetType().Name == "TenantBillingDriftAcknowledged");
    }

    [Fact]
    public async Task AcknowledgeBillingDrift_WhenSubscriptionHasNoDrift_ShouldReturnBadRequest()
    {
        // Arrange
        var identity = MockEasyAuthIdentities.Default.Single(i => i.Id == "admin");
        using var client = CreateBackOfficeClientForIdentity(identity);

        // Act
        var response = await client.PostAsync($"/api/back-office/tenants/{DatabaseSeeder.Tenant1.Id}/drift/acknowledge", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("Subscription has no drift to acknowledge.");
    }

    [Fact]
    public async Task AcknowledgeBillingDrift_WhenTenantNotFound_ShouldReturnNotFound()
    {
        // Arrange
        var identity = MockEasyAuthIdentities.Default.Single(i => i.Id == "admin");
        using var client = CreateBackOfficeClientForIdentity(identity);
        var unknownTenantId = TenantId.NewId();

        // Act
        var response = await client.PostAsync($"/api/back-office/tenants/{unknownTenantId}/drift/acknowledge", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task AcknowledgeBillingDrift_WhenNonAdminBackOfficeUser_ShouldReturnForbidden()
    {
        // Arrange
        var identity = MockEasyAuthIdentities.Default.Single(i => i.Id == "user");
        using var client = CreateBackOfficeClientForIdentity(identity);

        // Act
        var response = await client.PostAsync($"/api/back-office/tenants/{DatabaseSeeder.Tenant1.Id}/drift/acknowledge", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task AcknowledgeBillingDrift_WhenUnauthenticated_ShouldReturnUnauthorized()
    {
        // Arrange
        using var client = CreateBackOfficeClient();

        // Act
        var response = await client.PostAsync($"/api/back-office/tenants/{DatabaseSeeder.Tenant1.Id}/drift/acknowledge", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
