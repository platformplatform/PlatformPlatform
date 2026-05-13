using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Account.Features.Authentication.Domain;
using Account.Features.Subscriptions.Domain;
using Account.Features.Tenants.Domain;
using Account.Features.Users.BackOffice.Queries;
using Account.Features.Users.Domain;
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
        payload.Sessions.Should().OnlyContain(s => s.TenantId == DatabaseSeeder.Tenant1.Id);
        payload.Sessions.Should().OnlyContain(s => s.TenantName == DatabaseSeeder.Tenant1.Name);
        payload.Sessions.Should().OnlyContain(s => s.TenantLogoUrl == null);
    }

    [Fact]
    public async Task GetBackOfficeUserSessions_WhenUserBelongsToMultipleTenants_ShouldReturnSessionsAcrossAllTenants()
    {
        // Arrange
        var sharedEmail = "shared-sessions@example.com";
        var primaryUserId = SeedUser(DatabaseSeeder.Tenant1.Id, sharedEmail, UserRole.Member);
        var otherTenantId = SeedTenant("Other Sessions Tenant");
        var siblingUserId = SeedUser(otherTenantId, sharedEmail, UserRole.Owner);
        SeedSession(DatabaseSeeder.Tenant1.Id, primaryUserId, false);
        SeedSession(otherTenantId, siblingUserId, false);
        SeedSession(otherTenantId, siblingUserId, true);
        var identity = MockEasyAuthIdentities.Default.Single(i => i.Id == "user");
        using var client = CreateBackOfficeClientForIdentity(identity);

        // Act
        var response = await client.GetAsync($"/api/back-office/users/{primaryUserId}/sessions");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<BackOfficeUserSessionsResponse>();
        payload.Should().NotBeNull();
        payload.TotalCount.Should().Be(3);
        payload.Sessions.Should().Contain(s => s.TenantId == DatabaseSeeder.Tenant1.Id);
        payload.Sessions.Should().Contain(s => s.TenantId == otherTenantId);
        payload.Sessions.Should().Contain(s => s.TenantName == "Other Sessions Tenant");
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

    private TenantId SeedTenant(string name)
    {
        var tenantId = TenantId.NewId();
        Connection.Insert("tenants", [
                ("id", tenantId.Value),
                ("created_at", DateTimeOffset.UtcNow.AddDays(-30)),
                ("modified_at", null),
                ("name", name),
                ("state", nameof(TenantState.Active)),
                ("plan", nameof(SubscriptionPlan.Basis)),
                ("logo", """{"Url":null,"Version":0}"""),
                ("rollout_bucket", 50)
            ]
        );
        return tenantId;
    }

    private UserId SeedUser(TenantId tenantId, string email, UserRole role)
    {
        var userId = UserId.NewId();
        Connection.Insert("users", [
                ("tenant_id", tenantId.Value),
                ("id", userId.ToString()),
                ("created_at", DateTimeOffset.UtcNow.AddDays(-30)),
                ("modified_at", null),
                ("email", email),
                ("external_identities", "[]"),
                ("email_confirmed", true),
                ("first_name", null),
                ("last_name", null),
                ("title", null),
                ("role", role.ToString()),
                ("locale", "en-US"),
                ("avatar", JsonSerializer.Serialize(new Avatar())),
                ("rollout_bucket", 50)
            ]
        );
        return userId;
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
