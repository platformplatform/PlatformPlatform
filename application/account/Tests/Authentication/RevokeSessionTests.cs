using System.Net;
using Account.Database;
using Account.Features.Authentication.Domain;
using FluentAssertions;
using SharedKernel.Authentication.TokenGeneration;
using SharedKernel.Tests;
using SharedKernel.Tests.Persistence;
using Xunit;

namespace Account.Tests.Authentication;

public sealed class RevokeSessionTests : EndpointBaseTest<AccountDbContext>
{
    [Fact]
    public async Task RevokeSession_WhenValid_ShouldRevokeSession()
    {
        // Arrange
        var sessionId = InsertSession(DatabaseSeeder.Tenant1Owner.TenantId, DatabaseSeeder.Tenant1Owner.Id);

        // Act
        var response = await AuthenticatedOwnerHttpClient.DeleteAsync($"/api/account/authentication/sessions/{sessionId}");

        // Assert
        response.ShouldHaveEmptyHeaderAndLocationOnSuccess();
        object[] parameters = [new { id = sessionId }];
        Connection.ExecuteScalar<string>("SELECT revoked_at FROM sessions WHERE id = @id", parameters).Should().NotBeNull();
        Connection.ExecuteScalar<string>("SELECT revoked_reason FROM sessions WHERE id = @id", parameters).Should().Be("Revoked");

        TelemetryEventsCollectorSpy.CollectedEvents.Count.Should().Be(1);
        TelemetryEventsCollectorSpy.CollectedEvents[0].GetType().Name.Should().Be("SessionRevoked");
        TelemetryEventsCollectorSpy.CollectedEvents[0].Properties["event.reason"].Should().Be("Revoked");
        TelemetryEventsCollectorSpy.AreAllEventsDispatched.Should().BeTrue();
    }

    [Fact]
    public async Task RevokeSession_WhenSessionNotFound_ShouldReturnNotFound()
    {
        // Arrange
        var nonExistentSessionId = SessionId.NewId();

        // Act
        var response = await AuthenticatedOwnerHttpClient.DeleteAsync($"/api/account/authentication/sessions/{nonExistentSessionId}");

        // Assert
        await response.ShouldHaveErrorStatusCode(HttpStatusCode.NotFound, $"Session with id '{nonExistentSessionId}' not found.");

        TelemetryEventsCollectorSpy.CollectedEvents.Should().BeEmpty();
    }

    [Fact]
    public async Task RevokeSession_WhenSessionBelongsToOtherUser_ShouldReturnForbidden()
    {
        // Arrange
        var otherUserSessionId = InsertSession(DatabaseSeeder.Tenant1Member.TenantId, DatabaseSeeder.Tenant1Member.Id);

        // Act
        var response = await AuthenticatedOwnerHttpClient.DeleteAsync($"/api/account/authentication/sessions/{otherUserSessionId}");

        // Assert
        await response.ShouldHaveErrorStatusCode(HttpStatusCode.Forbidden, "You can only revoke your own sessions.");

        TelemetryEventsCollectorSpy.CollectedEvents.Should().BeEmpty();
    }

    [Fact]
    public async Task RevokeSession_WhenSessionAlreadyRevoked_ShouldReturnBadRequest()
    {
        // Arrange
        var sessionId = InsertSession(DatabaseSeeder.Tenant1Owner.TenantId, DatabaseSeeder.Tenant1Owner.Id, true);

        // Act
        var response = await AuthenticatedOwnerHttpClient.DeleteAsync($"/api/account/authentication/sessions/{sessionId}");

        // Assert
        await response.ShouldHaveErrorStatusCode(HttpStatusCode.BadRequest, $"Session with id '{sessionId}' is already revoked.");

        TelemetryEventsCollectorSpy.CollectedEvents.Should().BeEmpty();
    }

    [Fact]
    public async Task RevokeSession_WhenNotAuthenticated_ShouldReturnUnauthorized()
    {
        // Arrange
        var sessionId = SessionId.NewId();

        // Act
        var response = await AnonymousHttpClient.DeleteAsync($"/api/account/authentication/sessions/{sessionId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
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
