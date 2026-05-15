using System.Net;
using System.Net.Http.Json;
using Account.Features.Users.BackOffice.Commands;
using Account.Tests.BackOffice;
using FluentAssertions;
using SharedKernel.Authentication.MockEasyAuth;
using SharedKernel.Domain;
using SharedKernel.FeatureFlags;
using SharedKernel.Tests.Persistence;
using Xunit;

namespace Account.Tests.Users.BackOffice;

public sealed class SetUserAbInclusionPinTests : BackOfficeEndpointBaseTest
{
    [Fact]
    public async Task SetUserAbInclusionPin_WhenAlwaysOn_ShouldPersistPin()
    {
        // Arrange
        var identity = MockEasyAuthIdentities.Default.Single(i => i.Id == "admin");
        using var client = CreateBackOfficeClientForIdentity(identity);
        var command = new SetUserAbInclusionPinCommand { AbInclusionPin = AbInclusionPin.AlwaysOn };

        // Act
        var response = await client.PutAsJsonAsync($"/api/back-office/users/{DatabaseSeeder.Tenant1Owner.Id}/ab-inclusion-pin", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var storedPin = Connection.ExecuteScalar<string>(
            "SELECT ab_inclusion_pin FROM users WHERE id = @id", [new { id = DatabaseSeeder.Tenant1Owner.Id.ToString() }]
        );
        storedPin.Should().Be(nameof(AbInclusionPin.AlwaysOn));

        TelemetryEventsCollectorSpy.CollectedEvents.Should().ContainSingle(e => e.GetType().Name == "UserAbInclusionPinUpdated");
        TelemetryEventsCollectorSpy.CollectedEvents[0].Properties["event.from_pin"].Should().Be("none");
        TelemetryEventsCollectorSpy.CollectedEvents[0].Properties["event.to_pin"].Should().Be("AlwaysOn");
    }

    [Fact]
    public async Task SetUserAbInclusionPin_WhenNullClearsExistingPin_ShouldPersistNull()
    {
        // Arrange
        Connection.Update("users", "id", DatabaseSeeder.Tenant1Owner.Id.ToString(), [("ab_inclusion_pin", nameof(AbInclusionPin.NeverOn))]);
        var identity = MockEasyAuthIdentities.Default.Single(i => i.Id == "admin");
        using var client = CreateBackOfficeClientForIdentity(identity);
        var command = new SetUserAbInclusionPinCommand { AbInclusionPin = null };

        // Act
        var response = await client.PutAsJsonAsync($"/api/back-office/users/{DatabaseSeeder.Tenant1Owner.Id}/ab-inclusion-pin", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var storedPin = Connection.ExecuteScalar<string?>(
            "SELECT ab_inclusion_pin FROM users WHERE id = @id", [new { id = DatabaseSeeder.Tenant1Owner.Id.ToString() }]
        );
        storedPin.Should().BeNull();
    }

    [Fact]
    public async Task SetUserAbInclusionPin_WhenUserNotFound_ShouldReturnNotFound()
    {
        // Arrange
        var identity = MockEasyAuthIdentities.Default.Single(i => i.Id == "admin");
        using var client = CreateBackOfficeClientForIdentity(identity);
        var unknownUserId = UserId.NewId();
        var command = new SetUserAbInclusionPinCommand { AbInclusionPin = AbInclusionPin.AlwaysOn };

        // Act
        var response = await client.PutAsJsonAsync($"/api/back-office/users/{unknownUserId}/ab-inclusion-pin", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task SetUserAbInclusionPin_WhenNeverOn_ShouldFlipEvaluatorOutcomeForInRangeAbFlag()
    {
        // Arrange - put the user at bucket 50, with a rollout of 40..60. Flag is normally ON.
        Connection.Update("users", "id", DatabaseSeeder.Tenant1Owner.Id.ToString(), [("rollout_bucket", (short)50)]);
        var now = DateTimeOffset.UtcNow;
        Connection.Update("feature_flags", "flag_key", "experimental-ui", [
                ("enabled_at", now),
                ("bucket_start", 40),
                ("bucket_end", 60)
            ]
        );

        var identity = MockEasyAuthIdentities.Default.Single(i => i.Id == "admin");
        using var client = CreateBackOfficeClientForIdentity(identity);

        // Act - flag is enabled without pin
        var beforeResponse = await client.GetAsync($"/api/back-office/users/{DatabaseSeeder.Tenant1Owner.Id}/feature-flags");
        beforeResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var beforeBody = await beforeResponse.Content.ReadAsStringAsync();
        beforeBody.Should().Contain("\"flagKey\":\"experimental-ui\"");
        beforeBody.Should().Contain("\"isEnabled\":true");

        // Apply NeverOn pin
        var command = new SetUserAbInclusionPinCommand { AbInclusionPin = AbInclusionPin.NeverOn };
        var pinResponse = await client.PutAsJsonAsync($"/api/back-office/users/{DatabaseSeeder.Tenant1Owner.Id}/ab-inclusion-pin", command);
        pinResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Assert - flag is disabled despite being in bucket range
        var afterResponse = await client.GetAsync($"/api/back-office/users/{DatabaseSeeder.Tenant1Owner.Id}/feature-flags");
        afterResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var afterBody = await afterResponse.Content.ReadAsStringAsync();
        afterBody.Should().Contain("\"flagKey\":\"experimental-ui\"");
        afterBody.Should().Contain("\"isEnabled\":false");
    }

    [Fact]
    public async Task SetUserAbInclusionPin_WhenNonAdminBackOfficeUser_ShouldReturnForbidden()
    {
        // Arrange
        var identity = MockEasyAuthIdentities.Default.Single(i => i.Id == "user");
        using var client = CreateBackOfficeClientForIdentity(identity);
        var command = new SetUserAbInclusionPinCommand { AbInclusionPin = AbInclusionPin.AlwaysOn };

        // Act
        var response = await client.PutAsJsonAsync($"/api/back-office/users/{DatabaseSeeder.Tenant1Owner.Id}/ab-inclusion-pin", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task SetUserAbInclusionPin_WhenUnauthenticated_ShouldReturnUnauthorized()
    {
        // Arrange
        using var client = CreateBackOfficeClient();
        var command = new SetUserAbInclusionPinCommand { AbInclusionPin = AbInclusionPin.AlwaysOn };

        // Act
        var response = await client.PutAsJsonAsync($"/api/back-office/users/{DatabaseSeeder.Tenant1Owner.Id}/ab-inclusion-pin", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
