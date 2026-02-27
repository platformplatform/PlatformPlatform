using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using PlatformPlatform.Account.Database;
using PlatformPlatform.Account.Features.Users.Commands;
using PlatformPlatform.SharedKernel.Tests;
using Xunit;

namespace PlatformPlatform.Account.Tests.Users;

public sealed class ChangeZoomLevelTests : EndpointBaseTest<AccountDbContext>
{
    [Fact]
    public async Task ChangeZoomLevel_WhenValid_ShouldCollectTelemetryEvent()
    {
        // Arrange
        var command = new ChangeZoomLevelCommand("1", "1.25");

        // Act
        var response = await AuthenticatedOwnerHttpClient.PutAsJsonAsync("/api/account/users/me/change-zoom-level", command);

        // Assert
        response.ShouldHaveEmptyHeaderAndLocationOnSuccess();

        TelemetryEventsCollectorSpy.CollectedEvents.Count.Should().Be(1);
        TelemetryEventsCollectorSpy.CollectedEvents[0].GetType().Name.Should().Be("UserZoomLevelChanged");
        TelemetryEventsCollectorSpy.CollectedEvents[0].Properties["event.from_zoom_level"].Should().Be("1");
        TelemetryEventsCollectorSpy.CollectedEvents[0].Properties["event.to_zoom_level"].Should().Be("1.25");
        TelemetryEventsCollectorSpy.AreAllEventsDispatched.Should().BeTrue();
    }

    [Fact]
    public async Task ChangeZoomLevel_WhenMemberChangesZoomLevel_ShouldSucceed()
    {
        // Arrange
        var command = new ChangeZoomLevelCommand("1", "0.875");

        // Act
        var response = await AuthenticatedMemberHttpClient.PutAsJsonAsync("/api/account/users/me/change-zoom-level", command);

        // Assert
        response.ShouldHaveEmptyHeaderAndLocationOnSuccess();

        TelemetryEventsCollectorSpy.CollectedEvents.Count.Should().Be(1);
        TelemetryEventsCollectorSpy.CollectedEvents[0].GetType().Name.Should().Be("UserZoomLevelChanged");
        TelemetryEventsCollectorSpy.AreAllEventsDispatched.Should().BeTrue();
    }

    [Fact]
    public async Task ChangeZoomLevel_WhenNotAuthenticated_ShouldReturnUnauthorized()
    {
        // Arrange
        var command = new ChangeZoomLevelCommand("1", "1.25");

        // Act
        var response = await AnonymousHttpClient.PutAsJsonAsync("/api/account/users/me/change-zoom-level", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        TelemetryEventsCollectorSpy.CollectedEvents.Should().BeEmpty();
        TelemetryEventsCollectorSpy.AreAllEventsDispatched.Should().BeFalse();
    }

    [Fact]
    public async Task ChangeZoomLevel_WhenChangingToSameValue_ShouldSucceed()
    {
        // Arrange
        var command = new ChangeZoomLevelCommand("1", "1");

        // Act
        var response = await AuthenticatedOwnerHttpClient.PutAsJsonAsync("/api/account/users/me/change-zoom-level", command);

        // Assert
        response.ShouldHaveEmptyHeaderAndLocationOnSuccess();

        TelemetryEventsCollectorSpy.CollectedEvents.Count.Should().Be(1);
        TelemetryEventsCollectorSpy.CollectedEvents[0].GetType().Name.Should().Be("UserZoomLevelChanged");
        TelemetryEventsCollectorSpy.CollectedEvents[0].Properties["event.from_zoom_level"].Should().Be("1");
        TelemetryEventsCollectorSpy.CollectedEvents[0].Properties["event.to_zoom_level"].Should().Be("1");
        TelemetryEventsCollectorSpy.AreAllEventsDispatched.Should().BeTrue();
    }
}
