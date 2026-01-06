using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using PlatformPlatform.AccountManagement.Database;
using PlatformPlatform.AccountManagement.Features.Authentication.Commands;
using PlatformPlatform.SharedKernel.Tests;
using PlatformPlatform.SharedKernel.Tests.Persistence;
using Xunit;

namespace PlatformPlatform.AccountManagement.Tests.Authentication;

public sealed class LogoutTests : EndpointBaseTest<AccountManagementDbContext>
{
    [Fact]
    public async Task Logout_WhenAuthenticatedAsOwner_ShouldRevokeSessionAndCollectLogoutEvent()
    {
        // Arrange
        var sessionId = DatabaseSeeder.Tenant1OwnerSession.Id.ToString();
        Connection.RowExists("Sessions", sessionId).Should().BeTrue();
        object[] parameters = [new { id = sessionId }];
        Connection.ExecuteScalar<string?>("SELECT RevokedAt FROM Sessions WHERE Id = @id", parameters).Should().BeNull();
        var command = new LogoutCommand();

        // Act
        var response = await AuthenticatedOwnerHttpClient.PostAsJsonAsync("/api/account-management/authentication/logout", command);

        // Assert
        await response.ShouldBeSuccessfulPostRequest(hasLocation: false);
        Connection.ExecuteScalar<string?>("SELECT RevokedAt FROM Sessions WHERE Id = @id", parameters).Should().NotBeNull();
        Connection.ExecuteScalar<string?>("SELECT RevokedReason FROM Sessions WHERE Id = @id", parameters).Should().Be("LoggedOut");
        TelemetryEventsCollectorSpy.CollectedEvents.Count.Should().Be(2);
        TelemetryEventsCollectorSpy.CollectedEvents[0].GetType().Name.Should().Be("SessionRevoked");
        TelemetryEventsCollectorSpy.CollectedEvents[0].Properties["event.reason"].Should().Be("LoggedOut");
        TelemetryEventsCollectorSpy.CollectedEvents[1].GetType().Name.Should().Be("Logout");
        TelemetryEventsCollectorSpy.AreAllEventsDispatched.Should().BeTrue();
    }

    [Fact]
    public async Task Logout_WhenAuthenticatedAsMember_ShouldRevokeSessionAndCollectLogoutEvent()
    {
        // Arrange
        var sessionId = DatabaseSeeder.Tenant1MemberSession.Id.ToString();
        Connection.RowExists("Sessions", sessionId).Should().BeTrue();
        object[] parameters = [new { id = sessionId }];
        Connection.ExecuteScalar<string?>("SELECT RevokedAt FROM Sessions WHERE Id = @id", parameters).Should().BeNull();
        var command = new LogoutCommand();

        // Act
        var response = await AuthenticatedMemberHttpClient.PostAsJsonAsync("/api/account-management/authentication/logout", command);

        // Assert
        await response.ShouldBeSuccessfulPostRequest(hasLocation: false);
        Connection.ExecuteScalar<string?>("SELECT RevokedAt FROM Sessions WHERE Id = @id", parameters).Should().NotBeNull();
        Connection.ExecuteScalar<string?>("SELECT RevokedReason FROM Sessions WHERE Id = @id", parameters).Should().Be("LoggedOut");
        TelemetryEventsCollectorSpy.CollectedEvents.Count.Should().Be(2);
        TelemetryEventsCollectorSpy.CollectedEvents[0].GetType().Name.Should().Be("SessionRevoked");
        TelemetryEventsCollectorSpy.CollectedEvents[0].Properties["event.reason"].Should().Be("LoggedOut");
        TelemetryEventsCollectorSpy.CollectedEvents[1].GetType().Name.Should().Be("Logout");
        TelemetryEventsCollectorSpy.AreAllEventsDispatched.Should().BeTrue();
    }

    [Fact]
    public async Task Logout_WhenNotAuthenticated_ShouldReturnUnauthorized()
    {
        // Arrange
        var command = new LogoutCommand();

        // Act
        var response = await AnonymousHttpClient.PostAsJsonAsync("/api/account-management/authentication/logout", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        TelemetryEventsCollectorSpy.CollectedEvents.Should().BeEmpty();
        TelemetryEventsCollectorSpy.AreAllEventsDispatched.Should().BeFalse();
    }
}
