using System.Net;
using FluentAssertions;
using PlatformPlatform.AccountManagement.Database;
using PlatformPlatform.AccountManagement.Features.Authentication.Domain;
using PlatformPlatform.AccountManagement.Features.Authentication.Queries;
using PlatformPlatform.SharedKernel.Authentication.TokenGeneration;
using PlatformPlatform.SharedKernel.Domain;
using PlatformPlatform.SharedKernel.Tests;
using PlatformPlatform.SharedKernel.Tests.Persistence;
using Xunit;

namespace PlatformPlatform.AccountManagement.Tests.Authentication;

public sealed class GetUserSessionsTests : EndpointBaseTest<AccountManagementDbContext>
{
    [Fact]
    public async Task GetUserSessions_WhenUserHasSessions_ShouldReturnSessions()
    {
        // Arrange
        var sessionId1 = InsertSession(DatabaseSeeder.Tenant1Owner.TenantId, DatabaseSeeder.Tenant1Owner.Id);
        var sessionId2 = InsertSession(DatabaseSeeder.Tenant1Owner.TenantId, DatabaseSeeder.Tenant1Owner.Id);

        // Act
        var response = await AuthenticatedOwnerHttpClient.GetAsync("/api/account-management/authentication/sessions");

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
        var response = await AuthenticatedOwnerHttpClient.GetAsync("/api/account-management/authentication/sessions");

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
        var response = await AnonymousHttpClient.GetAsync("/api/account-management/authentication/sessions");

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
        var response = await AuthenticatedOwnerHttpClient.GetAsync("/api/account-management/authentication/sessions");

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
        var response = await AuthenticatedOwnerHttpClient.GetAsync("/api/account-management/authentication/sessions");

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
        var response = await AuthenticatedOwnerHttpClient.GetAsync("/api/account-management/authentication/sessions");

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

        Connection.Insert("Tenants", [
                ("Id", tenantId),
                ("CreatedAt", now),
                ("ModifiedAt", null),
                ("Name", name),
                ("State", "Active"),
                ("Logo", """{"Url":null,"Version":0}""")
            ]
        );

        return tenantId;
    }

    private void InsertUser(long tenantId, UserId userId, string email)
    {
        var now = TimeProvider.System.GetUtcNow();

        Connection.Insert("Users", [
                ("TenantId", tenantId),
                ("Id", userId.ToString()),
                ("CreatedAt", now),
                ("ModifiedAt", null),
                ("Email", email),
                ("EmailConfirmed", true),
                ("FirstName", "Test"),
                ("LastName", "User"),
                ("Title", null),
                ("Avatar", """{"Url":null,"Version":0,"IsGravatar":false}"""),
                ("Role", "Owner"),
                ("Locale", "en-US")
            ]
        );
    }

    private string InsertSession(long tenantId, string userId, bool isRevoked = false)
    {
        var sessionId = SessionId.NewId().ToString();
        var jti = RefreshTokenJti.NewId().ToString();
        var now = TimeProvider.System.GetUtcNow();

        Connection.Insert("Sessions", [
                ("TenantId", tenantId),
                ("Id", sessionId),
                ("UserId", userId),
                ("CreatedAt", now),
                ("ModifiedAt", null),
                ("RefreshTokenJti", jti),
                ("PreviousRefreshTokenJti", null),
                ("RefreshTokenVersion", 1),
                ("DeviceType", nameof(DeviceType.Desktop)),
                ("UserAgent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64)"),
                ("IpAddress", "127.0.0.1"),
                ("RevokedAt", isRevoked ? now : null),
                ("RevokedReason", null)
            ]
        );

        return sessionId;
    }
}
