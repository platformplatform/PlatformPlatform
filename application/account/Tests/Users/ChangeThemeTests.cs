using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using PlatformPlatform.Account.Database;
using PlatformPlatform.Account.Features.Users.Commands;
using PlatformPlatform.SharedKernel.Tests;
using Xunit;

namespace PlatformPlatform.Account.Tests.Users;

public sealed class ChangeThemeTests : EndpointBaseTest<AccountDbContext>
{
    [Fact]
    public async Task ChangeTheme_WhenValid_ShouldCollectTelemetryEvent()
    {
        // Arrange
        var command = new ChangeThemeCommand("system", "dark", null);

        // Act
        var response = await AuthenticatedOwnerHttpClient.PutAsJsonAsync("/api/account/users/me/change-theme", command);

        // Assert
        response.ShouldHaveEmptyHeaderAndLocationOnSuccess();

        TelemetryEventsCollectorSpy.CollectedEvents.Count.Should().Be(1);
        TelemetryEventsCollectorSpy.CollectedEvents[0].GetType().Name.Should().Be("UserThemeChanged");
        TelemetryEventsCollectorSpy.CollectedEvents[0].Properties["event.from_theme"].Should().Be("system");
        TelemetryEventsCollectorSpy.CollectedEvents[0].Properties["event.to_theme"].Should().Be("dark");
        TelemetryEventsCollectorSpy.CollectedEvents[0].Properties["event.resolved_theme"].Should().Be("none");
        TelemetryEventsCollectorSpy.AreAllEventsDispatched.Should().BeTrue();
    }

    [Fact]
    public async Task ChangeTheme_WhenSystemThemeWithResolvedTheme_ShouldTrackResolvedTheme()
    {
        // Arrange
        var command = new ChangeThemeCommand("dark", "system", "light");

        // Act
        var response = await AuthenticatedOwnerHttpClient.PutAsJsonAsync("/api/account/users/me/change-theme", command);

        // Assert
        response.ShouldHaveEmptyHeaderAndLocationOnSuccess();

        TelemetryEventsCollectorSpy.CollectedEvents.Count.Should().Be(1);
        TelemetryEventsCollectorSpy.CollectedEvents[0].GetType().Name.Should().Be("UserThemeChanged");
        TelemetryEventsCollectorSpy.CollectedEvents[0].Properties["event.from_theme"].Should().Be("dark");
        TelemetryEventsCollectorSpy.CollectedEvents[0].Properties["event.to_theme"].Should().Be("system");
        TelemetryEventsCollectorSpy.CollectedEvents[0].Properties["event.resolved_theme"].Should().Be("light");
        TelemetryEventsCollectorSpy.AreAllEventsDispatched.Should().BeTrue();
    }

    [Fact]
    public async Task ChangeTheme_WhenMemberChangesTheme_ShouldSucceed()
    {
        // Arrange
        var command = new ChangeThemeCommand("system", "light", null);

        // Act
        var response = await AuthenticatedMemberHttpClient.PutAsJsonAsync("/api/account/users/me/change-theme", command);

        // Assert
        response.ShouldHaveEmptyHeaderAndLocationOnSuccess();

        TelemetryEventsCollectorSpy.CollectedEvents.Count.Should().Be(1);
        TelemetryEventsCollectorSpy.CollectedEvents[0].GetType().Name.Should().Be("UserThemeChanged");
        TelemetryEventsCollectorSpy.AreAllEventsDispatched.Should().BeTrue();
    }

    [Fact]
    public async Task ChangeTheme_WhenNotAuthenticated_ShouldReturnUnauthorized()
    {
        // Arrange
        var command = new ChangeThemeCommand("system", "dark", null);

        // Act
        var response = await AnonymousHttpClient.PutAsJsonAsync("/api/account/users/me/change-theme", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        TelemetryEventsCollectorSpy.CollectedEvents.Should().BeEmpty();
        TelemetryEventsCollectorSpy.AreAllEventsDispatched.Should().BeFalse();
    }

    [Fact]
    public async Task ChangeTheme_WhenChangingToSameTheme_ShouldSucceed()
    {
        // Arrange
        var command = new ChangeThemeCommand("dark", "dark", null);

        // Act
        var response = await AuthenticatedOwnerHttpClient.PutAsJsonAsync("/api/account/users/me/change-theme", command);

        // Assert
        response.ShouldHaveEmptyHeaderAndLocationOnSuccess();

        TelemetryEventsCollectorSpy.CollectedEvents.Count.Should().Be(1);
        TelemetryEventsCollectorSpy.CollectedEvents[0].GetType().Name.Should().Be("UserThemeChanged");
        TelemetryEventsCollectorSpy.CollectedEvents[0].Properties["event.from_theme"].Should().Be("dark");
        TelemetryEventsCollectorSpy.CollectedEvents[0].Properties["event.to_theme"].Should().Be("dark");
        TelemetryEventsCollectorSpy.AreAllEventsDispatched.Should().BeTrue();
    }
}
