using System.Net;
using Account.Database;
using Account.Features.Authentication.Domain;
using Account.Features.Authentication.Queries;
using Account.Features.Subscriptions.Domain;
using FluentAssertions;
using SharedKernel.Authentication.TokenGeneration;
using SharedKernel.Domain;
using SharedKernel.Tests;
using SharedKernel.Tests.Persistence;
using Xunit;

namespace Account.Tests.Authentication;

public sealed class GetUserSessionsTests : EndpointBaseTest<AccountDbContext>
{
    [Fact]
    public async Task GetUserSessions_WhenUserHasSessions_ShouldReturnSessions()
    {
        // Arrange
        var sessionId1 = InsertSession(DatabaseSeeder.Tenant1Owner.TenantId, DatabaseSeeder.Tenant1Owner.Id);
        var sessionId2 = InsertSession(DatabaseSeeder.Tenant1Owner.TenantId, DatabaseSeeder.Tenant1Owner.Id);

        // Act
        var response = await AuthenticatedOwnerHttpClient.GetAsync("/api/account/authentication/sessions");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var responseBody = await response.DeserializeResponse<UserSessionsResponse>();
        responseBody.Should().NotBeNull();
        responseBody.Sessions.Length.Should().Be(3);
        responseBody.Sessions.Should().Contain(s => s.Id == new SessionId(sessionId1));
        responseBody.Sessions.Should().Contain(s => s.Id == new SessionId(sessionId2));
        responseBody.Sessions.Should().Contain(s => s.Id == DatabaseSeeder.Tenant1OwnerSession.Id);
    }

    [Fact]
    public async Task GetUserSessions_WhenOnlySeededSession_ShouldReturnSeededSession()
    {
        // Act
        var response = await AuthenticatedOwnerHttpClient.GetAsync("/api/account/authentication/sessions");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var responseBody = await response.DeserializeResponse<UserSessionsResponse>();
        responseBody.Should().NotBeNull();
        responseBody.Sessions.Length.Should().Be(1);
        responseBody.Sessions[0].Id.Should().Be(DatabaseSeeder.Tenant1OwnerSession.Id);
    }

    [Fact]
    public async Task GetUserSessions_WhenNotAuthenticated_ShouldReturnUnauthorized()
    {
        // Act
        var response = await AnonymousHttpClient.GetAsync("/api/account/authentication/sessions");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetUserSessions_ShouldNotReturnOtherUserSessions()
    {
        // Arrange
        InsertSession(DatabaseSeeder.Tenant1Member.TenantId, DatabaseSeeder.Tenant1Member.Id);
        var ownerSessionId = InsertSession(DatabaseSeeder.Tenant1Owner.TenantId, DatabaseSeeder.Tenant1Owner.Id);

        // Act
        var response = await AuthenticatedOwnerHttpClient.GetAsync("/api/account/authentication/sessions");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var responseBody = await response.DeserializeResponse<UserSessionsResponse>();
        responseBody.Should().NotBeNull();
        responseBody.Sessions.Length.Should().Be(2);
        responseBody.Sessions.Should().Contain(s => s.Id == new SessionId(ownerSessionId));
        responseBody.Sessions.Should().Contain(s => s.Id == DatabaseSeeder.Tenant1OwnerSession.Id);
    }

    [Fact]
    public async Task GetUserSessions_ShouldReturnSessionsAcrossAllTenants()
    {
        // Arrange
        var tenant2Name = "Tenant 2";
        var tenant2Id = InsertTenant(tenant2Name);
        var user2Id = UserId.NewId();
        InsertUser(tenant2Id, user2Id, DatabaseSeeder.Tenant1Owner.Email);
        var tenant2SessionId = InsertSession(tenant2Id, user2Id);

        // Act
        var response = await AuthenticatedOwnerHttpClient.GetAsync("/api/account/authentication/sessions");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var responseBody = await response.DeserializeResponse<UserSessionsResponse>();
        responseBody.Should().NotBeNull();
        responseBody.Sessions.Length.Should().Be(2);
        responseBody.Sessions.Should().Contain(s => s.Id == DatabaseSeeder.Tenant1OwnerSession.Id);
        responseBody.Sessions.Should().Contain(s => s.Id == new SessionId(tenant2SessionId));

        var tenant1Session = responseBody.Sessions.Single(s => s.Id == DatabaseSeeder.Tenant1OwnerSession.Id);
        tenant1Session.TenantName.Should().Be(DatabaseSeeder.Tenant1.Name);

        var tenant2Session = responseBody.Sessions.Single(s => s.Id == new SessionId(tenant2SessionId));
        tenant2Session.TenantName.Should().Be(tenant2Name);
    }

    [Fact]
    public async Task GetUserSessions_ShouldNotReturnRevokedSessions()
    {
        // Arrange
        InsertSession(DatabaseSeeder.Tenant1Owner.TenantId, DatabaseSeeder.Tenant1Owner.Id, true);
        var activeSessionId = InsertSession(DatabaseSeeder.Tenant1Owner.TenantId, DatabaseSeeder.Tenant1Owner.Id);

        // Act
        var response = await AuthenticatedOwnerHttpClient.GetAsync("/api/account/authentication/sessions");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var responseBody = await response.DeserializeResponse<UserSessionsResponse>();
        responseBody.Should().NotBeNull();
        responseBody.Sessions.Length.Should().Be(2);
        responseBody.Sessions.Should().Contain(s => s.Id == new SessionId(activeSessionId));
        responseBody.Sessions.Should().Contain(s => s.Id == DatabaseSeeder.Tenant1OwnerSession.Id);
    }

    private long InsertTenant(string name)
    {
        var tenantId = TenantId.NewId().Value;
        var now = TimeProvider.System.GetUtcNow();

        Connection.Insert("tenants", [
                ("id", tenantId),
                ("created_at", now),
                ("modified_at", null),
                ("name", name),
                ("state", "Active"),
                ("logo", """{"Url":null,"Version":0}"""),
                ("plan", nameof(SubscriptionPlan.Basis)),
                ("rollout_bucket", 42)
            ]
        );

        return tenantId;
    }

    private void InsertUser(long tenantId, UserId userId, string email)
    {
        var now = TimeProvider.System.GetUtcNow();

        Connection.Insert("users", [
                ("tenant_id", tenantId),
                ("id", userId.ToString()),
                ("created_at", now),
                ("modified_at", null),
                ("email", email),
                ("email_confirmed", true),
                ("first_name", "Test"),
                ("last_name", "User"),
                ("title", null),
                ("avatar", """{"Url":null,"Version":0,"IsGravatar":false}"""),
                ("role", "Owner"),
                ("locale", "en-US"),
                ("external_identities", "[]"),
                ("rollout_bucket", 42)
            ]
        );
    }

    private string InsertSession(long tenantId, string userId, bool isRevoked = false)
    {
        var sessionId = SessionId.NewId().ToString();
        var jti = RefreshTokenJti.NewId().ToString();
        var now = TimeProvider.System.GetUtcNow();

        Connection.Insert("sessions", [
                ("tenant_id", tenantId),
                ("id", sessionId),
                ("user_id", userId),
                ("created_at", now),
                ("modified_at", null),
                ("refresh_token_jti", jti),
                ("previous_refresh_token_jti", null),
                ("refresh_token_version", 1),
                ("login_method", nameof(LoginMethod.OneTimePassword)),
                ("device_type", nameof(DeviceType.Desktop)),
                ("user_agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64)"),
                ("ip_address", "127.0.0.1"),
                ("revoked_at", isRevoked ? now : null),
                ("revoked_reason", null)
            ]
        );

        return sessionId;
    }
}
