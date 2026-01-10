using System.Net;
using System.Net.Http.Headers;
using FluentAssertions;
using Mapster;
using Microsoft.Extensions.DependencyInjection;
using PlatformPlatform.AccountManagement.Database;
using PlatformPlatform.AccountManagement.Features.Authentication.Domain;
using PlatformPlatform.SharedKernel.Authentication;
using PlatformPlatform.SharedKernel.Authentication.TokenGeneration;
using PlatformPlatform.SharedKernel.Tests.Persistence;
using Xunit;

namespace PlatformPlatform.AccountManagement.Tests.Authentication;

public sealed class RefreshAuthenticationTokensTests : EndpointBaseTest<AccountManagementDbContext>
{
    private readonly RefreshTokenGenerator _refreshTokenGenerator;

    public RefreshAuthenticationTokensTests()
    {
        using var serviceScope = Provider.CreateScope();
        _refreshTokenGenerator = serviceScope.ServiceProvider.GetRequiredService<RefreshTokenGenerator>();
    }

    [Fact]
    public async Task RefreshAuthenticationTokens_WhenValidToken_ShouldRefreshAndIncrementVersion()
    {
        // Arrange
        var jti = RefreshTokenJti.NewId();
        var sessionId = SessionId.NewId();
        InsertSession(DatabaseSeeder.Tenant1Owner.TenantId, DatabaseSeeder.Tenant1Owner.Id, sessionId, jti, 1);
        var userInfo = DatabaseSeeder.Tenant1Owner.Adapt<UserInfo>();
        var refreshToken = _refreshTokenGenerator.Generate(userInfo, sessionId, jti);
        TelemetryEventsCollectorSpy.Reset();

        // Act
        var response = await SendRefreshRequest(refreshToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var updatedVersion = Connection.ExecuteScalar<long>("SELECT RefreshTokenVersion FROM Sessions WHERE Id = @id", [new { id = sessionId.ToString() }]);
        updatedVersion.Should().Be(2);
    }

    [Fact]
    public async Task RefreshAuthenticationTokens_WhenPreviousVersionWithinGracePeriod_ShouldSucceed()
    {
        // Arrange
        var previousJti = RefreshTokenJti.NewId();
        var currentJti = RefreshTokenJti.NewId();
        var sessionId = SessionId.NewId();
        var now = TimeProvider.System.GetUtcNow();
        InsertSessionWithGracePeriod(DatabaseSeeder.Tenant1Owner.TenantId, DatabaseSeeder.Tenant1Owner.Id, sessionId, currentJti, previousJti, 2, now.AddSeconds(-10));
        var userInfo = DatabaseSeeder.Tenant1Owner.Adapt<UserInfo>();
        var refreshToken = GenerateRefreshTokenWithVersion(userInfo, sessionId, previousJti, 1);
        TelemetryEventsCollectorSpy.Reset();

        // Act
        var response = await SendRefreshRequest(refreshToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var sessionVersion = Connection.ExecuteScalar<long>("SELECT RefreshTokenVersion FROM Sessions WHERE Id = @id", [new { id = sessionId.ToString() }]);
        sessionVersion.Should().Be(2);
    }

    [Fact]
    public async Task RefreshAuthenticationTokens_WhenReplayAttackDetected_ShouldRevokeSessionAndReturnUnauthorized()
    {
        // Arrange
        var oldJti = RefreshTokenJti.NewId();
        var currentJti = RefreshTokenJti.NewId();
        var sessionId = SessionId.NewId();
        var now = TimeProvider.System.GetUtcNow();
        InsertSessionWithGracePeriod(DatabaseSeeder.Tenant1Owner.TenantId, DatabaseSeeder.Tenant1Owner.Id, sessionId, currentJti, null, 3, now.AddSeconds(-30));
        var userInfo = DatabaseSeeder.Tenant1Owner.Adapt<UserInfo>();
        var refreshToken = GenerateRefreshTokenWithVersion(userInfo, sessionId, oldJti, 1);
        TelemetryEventsCollectorSpy.Reset();

        // Act
        var response = await SendRefreshRequest(refreshToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        object[] parameters = [new { id = sessionId.ToString() }];
        Connection.ExecuteScalar<string>("SELECT RevokedAt FROM Sessions WHERE Id = @id", parameters).Should().NotBeNull();
        Connection.ExecuteScalar<string>("SELECT RevokedReason FROM Sessions WHERE Id = @id", parameters).Should().Be("ReplayAttackDetected");

        TelemetryEventsCollectorSpy.CollectedEvents.Count.Should().Be(1);
        TelemetryEventsCollectorSpy.CollectedEvents[0].GetType().Name.Should().Be("SessionReplayDetected");
        TelemetryEventsCollectorSpy.AreAllEventsDispatched.Should().BeTrue();
    }

    [Fact]
    public async Task RefreshAuthenticationTokens_WhenSessionRevoked_ShouldReturnUnauthorized()
    {
        // Arrange
        var jti = RefreshTokenJti.NewId();
        var sessionId = SessionId.NewId();
        InsertSession(DatabaseSeeder.Tenant1Owner.TenantId, DatabaseSeeder.Tenant1Owner.Id, sessionId, jti, 1, true);
        var userInfo = DatabaseSeeder.Tenant1Owner.Adapt<UserInfo>();
        var refreshToken = _refreshTokenGenerator.Generate(userInfo, sessionId, jti);
        TelemetryEventsCollectorSpy.Reset();

        // Act
        var response = await SendRefreshRequest(refreshToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        TelemetryEventsCollectorSpy.CollectedEvents.Should().BeEmpty();
    }

    [Fact]
    public async Task RefreshAuthenticationTokens_WhenSessionNotFound_ShouldReturnUnauthorized()
    {
        // Arrange
        var jti = RefreshTokenJti.NewId();
        var sessionId = SessionId.NewId();
        var userInfo = DatabaseSeeder.Tenant1Owner.Adapt<UserInfo>();
        var refreshToken = _refreshTokenGenerator.Generate(userInfo, sessionId, jti);
        TelemetryEventsCollectorSpy.Reset();

        // Act
        var response = await SendRefreshRequest(refreshToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        TelemetryEventsCollectorSpy.CollectedEvents.Should().BeEmpty();
    }

    private async Task<HttpResponseMessage> SendRefreshRequest(string refreshToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/internal-api/account-management/authentication/refresh-authentication-tokens");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", refreshToken);
        return await AnonymousHttpClient.SendAsync(request);
    }

    private string GenerateRefreshTokenWithVersion(UserInfo userInfo, SessionId sessionId, RefreshTokenJti jti, int version)
    {
        using var serviceScope = Provider.CreateScope();
        var generator = serviceScope.ServiceProvider.GetRequiredService<RefreshTokenGenerator>();
        var token = generator.Generate(userInfo, sessionId, jti);

        for (var i = 1; i < version; i++)
        {
            token = generator.Update(userInfo, sessionId, jti, i, TimeProvider.System.GetUtcNow().AddHours(2160));
        }

        return token;
    }

    private void InsertSession(long tenantId, string userId, SessionId sessionId, RefreshTokenJti jti, int version, bool isRevoked = false)
    {
        var now = TimeProvider.System.GetUtcNow();

        Connection.Insert("Sessions", [
                ("TenantId", tenantId),
                ("Id", sessionId.ToString()),
                ("UserId", userId),
                ("CreatedAt", now),
                ("ModifiedAt", null),
                ("RefreshTokenJti", jti.ToString()),
                ("PreviousRefreshTokenJti", null),
                ("RefreshTokenVersion", version),
                ("DeviceType", nameof(DeviceType.Desktop)),
                ("UserAgent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64)"),
                ("IpAddress", "127.0.0.1"),
                ("RevokedAt", isRevoked ? now : null),
                ("RevokedReason", null)
            ]
        );
    }

    private void InsertSessionWithGracePeriod(long tenantId, string userId, SessionId sessionId, RefreshTokenJti currentJti, RefreshTokenJti? previousJti, int currentVersion, DateTimeOffset modifiedAt)
    {
        var now = TimeProvider.System.GetUtcNow();

        Connection.Insert("Sessions", [
                ("TenantId", tenantId),
                ("Id", sessionId.ToString()),
                ("UserId", userId),
                ("CreatedAt", now),
                ("ModifiedAt", modifiedAt),
                ("RefreshTokenJti", currentJti.ToString()),
                ("PreviousRefreshTokenJti", previousJti?.ToString()),
                ("RefreshTokenVersion", currentVersion),
                ("DeviceType", nameof(DeviceType.Desktop)),
                ("UserAgent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64)"),
                ("IpAddress", "127.0.0.1"),
                ("RevokedAt", null),
                ("RevokedReason", null)
            ]
        );
    }
}
