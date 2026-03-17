using System.Net;
using System.Net.Http.Json;
using Account.Database;
using Account.Features.Authentication.Commands;
using FluentAssertions;
using SharedKernel.Tests;
using SharedKernel.Tests.Persistence;
using Xunit;

namespace Account.Tests.Authentication;

public sealed class LogoutTests : EndpointBaseTest<AccountDbContext>
{
    [Fact]
    public async Task Logout_WhenAuthenticatedAsOwner_ShouldRevokeSessionAndCollectLogoutEvent()
    {
        // Arrange
        var sessionId = DatabaseSeeder.Tenant1OwnerSession.Id.ToString();
        Connection.RowExists("sessions", sessionId).Should().BeTrue();
        object[] parameters = [new { id = sessionId }];
        Connection.ExecuteScalar<string?>("SELECT revoked_at FROM sessions WHERE id = @id", parameters).Should().BeNull();
        var command = new LogoutCommand();

        // Act
        var response = await AuthenticatedOwnerHttpClient.PostAsJsonAsync("/api/account/authentication/logout", command);

        // Assert
        await response.ShouldBeSuccessfulPostRequest(hasLocation: false);
        Connection.ExecuteScalar<string?>("SELECT revoked_at FROM sessions WHERE id = @id", parameters).Should().NotBeNull();
        Connection.ExecuteScalar<string?>("SELECT revoked_reason FROM sessions WHERE id = @id", parameters).Should().Be("LoggedOut");
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
        Connection.RowExists("sessions", sessionId).Should().BeTrue();
        object[] parameters = [new { id = sessionId }];
        Connection.ExecuteScalar<string?>("SELECT revoked_at FROM sessions WHERE id = @id", parameters).Should().BeNull();
        var command = new LogoutCommand();

        // Act
        var response = await AuthenticatedMemberHttpClient.PostAsJsonAsync("/api/account/authentication/logout", command);

        // Assert
        await response.ShouldBeSuccessfulPostRequest(hasLocation: false);
        Connection.ExecuteScalar<string?>("SELECT revoked_at FROM sessions WHERE id = @id", parameters).Should().NotBeNull();
        Connection.ExecuteScalar<string?>("SELECT revoked_reason FROM sessions WHERE id = @id", parameters).Should().Be("LoggedOut");
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
        var response = await AnonymousHttpClient.PostAsJsonAsync("/api/account/authentication/logout", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        TelemetryEventsCollectorSpy.CollectedEvents.Should().BeEmpty();
        TelemetryEventsCollectorSpy.AreAllEventsDispatched.Should().BeFalse();
    }
}
