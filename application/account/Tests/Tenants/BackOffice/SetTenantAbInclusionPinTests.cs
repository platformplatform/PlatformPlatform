using System.Net;
using System.Net.Http.Json;
using Account.Features.Tenants.BackOffice.Commands;
using Account.Tests.BackOffice;
using FluentAssertions;
using SharedKernel.Authentication.MockEasyAuth;
using SharedKernel.Domain;
using SharedKernel.FeatureFlags;
using SharedKernel.Tests.Persistence;
using Xunit;

namespace Account.Tests.Tenants.BackOffice;

public sealed class SetTenantAbInclusionPinTests : BackOfficeEndpointBaseTest
{
    [Fact]
    public async Task SetTenantAbInclusionPin_WhenAlwaysOn_ShouldPersistPin()
    {
        // Arrange
        var identity = MockEasyAuthIdentities.Default.Single(i => i.Id == "admin");
        using var client = CreateBackOfficeClientForIdentity(identity);
        var command = new SetTenantAbInclusionPinCommand { AbInclusionPin = AbInclusionPin.AlwaysOn };

        // Act
        var response = await client.PutAsJsonAsync($"/api/back-office/tenants/{DatabaseSeeder.Tenant1.Id}/ab-inclusion-pin", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var storedPin = Connection.ExecuteScalar<string>(
            "SELECT ab_inclusion_pin FROM tenants WHERE id = @id", [new { id = DatabaseSeeder.Tenant1.Id.Value }]
        );
        storedPin.Should().Be(nameof(AbInclusionPin.AlwaysOn));

        TelemetryEventsCollectorSpy.CollectedEvents.Should().ContainSingle(e => e.GetType().Name == "TenantAbInclusionPinUpdated");
        TelemetryEventsCollectorSpy.CollectedEvents[0].Properties["event.from_pin"].Should().Be("none");
        TelemetryEventsCollectorSpy.CollectedEvents[0].Properties["event.to_pin"].Should().Be("AlwaysOn");
    }

    [Fact]
    public async Task SetTenantAbInclusionPin_WhenNullClearsExistingPin_ShouldPersistNull()
    {
        // Arrange - seed an existing pin to verify null clears it
        Connection.Update("tenants", "id", DatabaseSeeder.Tenant1.Id.Value, [("ab_inclusion_pin", nameof(AbInclusionPin.AlwaysOn))]);
        var identity = MockEasyAuthIdentities.Default.Single(i => i.Id == "admin");
        using var client = CreateBackOfficeClientForIdentity(identity);
        var command = new SetTenantAbInclusionPinCommand { AbInclusionPin = null };

        // Act
        var response = await client.PutAsJsonAsync($"/api/back-office/tenants/{DatabaseSeeder.Tenant1.Id}/ab-inclusion-pin", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var storedPin = Connection.ExecuteScalar<string?>(
            "SELECT ab_inclusion_pin FROM tenants WHERE id = @id", [new { id = DatabaseSeeder.Tenant1.Id.Value }]
        );
        storedPin.Should().BeNull();
    }

    [Fact]
    public async Task SetTenantAbInclusionPin_WhenTenantNotFound_ShouldReturnNotFound()
    {
        // Arrange
        var identity = MockEasyAuthIdentities.Default.Single(i => i.Id == "admin");
        using var client = CreateBackOfficeClientForIdentity(identity);
        var unknownTenantId = TenantId.NewId();
        var command = new SetTenantAbInclusionPinCommand { AbInclusionPin = AbInclusionPin.AlwaysOn };

        // Act
        var response = await client.PutAsJsonAsync($"/api/back-office/tenants/{unknownTenantId}/ab-inclusion-pin", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task SetTenantAbInclusionPin_WhenAlwaysOn_ShouldFlipEvaluatorOutcomeForOutOfRangeAbFlag()
    {
        // Arrange - put the tenant at bucket 70 with a rollout of 40..60, so the flag is normally off
        Connection.Update("tenants", "id", DatabaseSeeder.Tenant1.Id.Value, [("rollout_bucket", (short)70)]);
        var now = DateTimeOffset.UtcNow;
        Connection.Update("feature_flags", "flag_key", "beta-features", [
                ("enabled_at", now),
                ("bucket_start", 40),
                ("bucket_end", 60)
            ]
        );

        var identity = MockEasyAuthIdentities.Default.Single(i => i.Id == "admin");
        using var client = CreateBackOfficeClientForIdentity(identity);

        // Act - first read flags without a pin: tenant is out of range, so flag is disabled
        var beforeResponse = await client.GetAsync($"/api/back-office/tenants/{DatabaseSeeder.Tenant1.Id}/feature-flags");
        beforeResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var beforeBody = await beforeResponse.Content.ReadAsStringAsync();
        beforeBody.Should().Contain("\"flagKey\":\"beta-features\"");
        beforeBody.Should().Contain("\"isEnabled\":false");

        // Apply AlwaysOn pin
        var command = new SetTenantAbInclusionPinCommand { AbInclusionPin = AbInclusionPin.AlwaysOn };
        var pinResponse = await client.PutAsJsonAsync($"/api/back-office/tenants/{DatabaseSeeder.Tenant1.Id}/ab-inclusion-pin", command);
        pinResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Assert - read flags again: tenant is pinned on, so flag is enabled despite being out of bucket range
        var afterResponse = await client.GetAsync($"/api/back-office/tenants/{DatabaseSeeder.Tenant1.Id}/feature-flags");
        afterResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var afterBody = await afterResponse.Content.ReadAsStringAsync();
        afterBody.Should().Contain("\"flagKey\":\"beta-features\"");
        afterBody.Should().Contain("\"isEnabled\":true");
    }

    [Fact]
    public async Task SetTenantAbInclusionPin_WhenNonAdminBackOfficeUser_ShouldReturnForbidden()
    {
        // Arrange
        var identity = MockEasyAuthIdentities.Default.Single(i => i.Id == "user");
        using var client = CreateBackOfficeClientForIdentity(identity);
        var command = new SetTenantAbInclusionPinCommand { AbInclusionPin = AbInclusionPin.AlwaysOn };

        // Act
        var response = await client.PutAsJsonAsync($"/api/back-office/tenants/{DatabaseSeeder.Tenant1.Id}/ab-inclusion-pin", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task SetTenantAbInclusionPin_WhenUnauthenticated_ShouldReturnUnauthorized()
    {
        // Arrange
        using var client = CreateBackOfficeClient();
        var command = new SetTenantAbInclusionPinCommand { AbInclusionPin = AbInclusionPin.AlwaysOn };

        // Act
        var response = await client.PutAsJsonAsync($"/api/back-office/tenants/{DatabaseSeeder.Tenant1.Id}/ab-inclusion-pin", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
