using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Account.Features.Authentication.Domain;
using Account.Features.Users.BackOffice.Queries;
using Account.Tests.BackOffice;
using FluentAssertions;
using SharedKernel.Authentication.BackOfficeIdentity;
using SharedKernel.Authentication.MockEasyAuth;
using SharedKernel.Authentication.TokenGeneration;
using SharedKernel.Domain;
using SharedKernel.Tests.Persistence;
using Xunit;

namespace Account.Tests.Users.BackOffice;

public sealed class GetBackOfficeUserSessionsTests : BackOfficeEndpointBaseTest
{
    [Fact]
    public async Task GetBackOfficeUserSessions_WhenUserHasSessions_ShouldReturnAllSessions()
    {
        // Arrange
        var user = DatabaseSeeder.Tenant1Owner;
        SeedSession(DatabaseSeeder.Tenant1.Id, user.Id, false);
        SeedSession(DatabaseSeeder.Tenant1.Id, user.Id, true);
        var identity = MockEasyAuthIdentities.Default.Single(i => i.Id == "user");
        using var client = CreateBackOfficeClientForIdentity(identity);

        // Act
        var response = await client.GetAsync($"/api/back-office/users/{user.Id}/sessions");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<BackOfficeUserSessionsResponse>();
        payload.Should().NotBeNull();
        // DatabaseSeeder seeds Tenant1OwnerSession, plus the two we just added.
        payload.TotalCount.Should().Be(3);
        payload.Sessions.Should().Contain(s => s.RevokedAt != null);
    }

    [Fact]
    public async Task GetBackOfficeUserSessions_WhenUserNotFound_ShouldReturnNotFound()
    {
        // Arrange
        var unknownUserId = UserId.NewId();
        var identity = MockEasyAuthIdentities.Default.Single(i => i.Id == "user");
        using var client = CreateBackOfficeClientForIdentity(identity);

        // Act
        var response = await client.GetAsync($"/api/back-office/users/{unknownUserId}/sessions");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetBackOfficeUserSessions_WhenCalledWithoutAuthentication_ShouldReturnUnauthorized()
    {
        // Arrange
        using var client = CreateBackOfficeClient();
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        // Act
        var response = await client.GetAsync($"/api/back-office/users/{DatabaseSeeder.Tenant1Owner.Id}/sessions");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetBackOfficeUserSessions_WhenCalledViaWrongHost_ShouldReturnNotFound()
    {
        // Arrange
        using var client = CreateClientForHost("app.test.localhost");
        client.DefaultRequestHeaders.Add(BackOfficeIdentityDefaults.PrincipalNameHeader, "Some User");

        // Act
        var response = await client.GetAsync($"/api/back-office/users/{DatabaseSeeder.Tenant1Owner.Id}/sessions");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private void SeedSession(TenantId tenantId, UserId userId, bool isRevoked)
    {
        var now = DateTimeOffset.UtcNow;
        Connection.Insert("sessions", [
                ("tenant_id", tenantId.Value),
                ("id", SessionId.NewId().ToString()),
                ("user_id", userId.ToString()),
                ("created_at", now.AddMinutes(-30)),
                ("modified_at", null),
                ("refresh_token_jti", RefreshTokenJti.NewId().ToString()),
                ("previous_refresh_token_jti", null),
                ("refresh_token_version", 1),
                ("login_method", nameof(LoginMethod.OneTimePassword)),
                ("device_type", nameof(DeviceType.Desktop)),
                ("user_agent", "Mozilla/5.0"),
                ("ip_address", "127.0.0.1"),
                ("revoked_at", isRevoked ? now : null),
                ("revoked_reason", isRevoked ? nameof(SessionRevokedReason.LoggedOut) : null)
            ]
        );
    }
}
