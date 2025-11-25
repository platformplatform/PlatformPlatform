using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using PlatformPlatform.AccountManagement.Database;
using PlatformPlatform.AccountManagement.Features.Users.Commands;
using PlatformPlatform.SharedKernel.Tests;
using PlatformPlatform.SharedKernel.Tests.Persistence;
using PlatformPlatform.SharedKernel.Validation;
using Xunit;

namespace PlatformPlatform.AccountManagement.Tests.Users;

public sealed class ChangeLocaleTests : EndpointBaseTest<AccountManagementDbContext>
{
    [Fact]
    public async Task ChangeLocale_WhenValidLocale_ShouldUpdateUserLocaleAndCollectEvent()
    {
        // Arrange
        var newLocale = "da-DK";
        var command = new ChangeLocaleCommand(newLocale);

        // Act
        var response = await AuthenticatedOwnerHttpClient.PutAsJsonAsync("/api/account-management/users/me/change-locale", command);

        // Assert
        response.ShouldHaveEmptyHeaderAndLocationOnSuccess();

        var updatedLocale = Connection.ExecuteScalar<string>(
            "SELECT Locale FROM Users WHERE Id = @id", [new { id = DatabaseSeeder.Tenant1Owner.Id.ToString() }]
        );
        updatedLocale.Should().Be(newLocale);

        TelemetryEventsCollectorSpy.CollectedEvents.Count.Should().Be(1);
        TelemetryEventsCollectorSpy.CollectedEvents[0].GetType().Name.Should().Be("UserLocaleChanged");
        TelemetryEventsCollectorSpy.CollectedEvents[0].Properties["event.from_locale"].Should().Be(string.Empty);
        TelemetryEventsCollectorSpy.CollectedEvents[0].Properties["event.to_locale"].Should().Be(newLocale);
        TelemetryEventsCollectorSpy.AreAllEventsDispatched.Should().BeTrue();
    }

    [Fact]
    public async Task ChangeLocale_WhenMemberChangesLocale_ShouldSucceed()
    {
        // Arrange
        var newLocale = "da-DK";
        var command = new ChangeLocaleCommand(newLocale);

        // Act
        var response = await AuthenticatedMemberHttpClient.PutAsJsonAsync("/api/account-management/users/me/change-locale", command);

        // Assert
        response.ShouldHaveEmptyHeaderAndLocationOnSuccess();

        var updatedLocale = Connection.ExecuteScalar<string>(
            "SELECT Locale FROM Users WHERE Id = @id", [new { id = DatabaseSeeder.Tenant1Member.Id.ToString() }]
        );
        updatedLocale.Should().Be(newLocale);

        TelemetryEventsCollectorSpy.CollectedEvents.Count.Should().Be(1);
        TelemetryEventsCollectorSpy.CollectedEvents[0].GetType().Name.Should().Be("UserLocaleChanged");
        TelemetryEventsCollectorSpy.AreAllEventsDispatched.Should().BeTrue();
    }

    [Fact]
    public async Task ChangeLocale_WhenInvalidLocale_ShouldReturnValidationError()
    {
        // Arrange
        var invalidLocale = "fr-FR";
        var command = new ChangeLocaleCommand(invalidLocale);

        // Act
        var response = await AuthenticatedOwnerHttpClient.PutAsJsonAsync("/api/account-management/users/me/change-locale", command);

        // Assert
        var expectedErrors = new[]
        {
            new ErrorDetail("locale", "Language must be one of the following: en-US, da-DK")
        };
        await response.ShouldHaveErrorStatusCode(HttpStatusCode.BadRequest, expectedErrors);

        TelemetryEventsCollectorSpy.CollectedEvents.Should().BeEmpty();
        TelemetryEventsCollectorSpy.AreAllEventsDispatched.Should().BeFalse();
    }

    [Fact]
    public async Task ChangeLocale_WhenNotAuthenticated_ShouldReturnUnauthorized()
    {
        // Arrange
        var command = new ChangeLocaleCommand("da-DK");

        // Act
        var response = await AnonymousHttpClient.PutAsJsonAsync("/api/account-management/users/me/change-locale", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        TelemetryEventsCollectorSpy.CollectedEvents.Should().BeEmpty();
        TelemetryEventsCollectorSpy.AreAllEventsDispatched.Should().BeFalse();
    }

    [Fact]
    public async Task ChangeLocale_WhenChangingToSameLocale_ShouldSucceed()
    {
        // Arrange
        var locale = "en-US";
        Connection.Update("Users", "Id", DatabaseSeeder.Tenant1Owner.Id.ToString(), [("Locale", locale)]);
        var command = new ChangeLocaleCommand(locale);

        // Act
        var response = await AuthenticatedOwnerHttpClient.PutAsJsonAsync("/api/account-management/users/me/change-locale", command);

        // Assert
        response.ShouldHaveEmptyHeaderAndLocationOnSuccess();

        var updatedLocale = Connection.ExecuteScalar<string>(
            "SELECT Locale FROM Users WHERE Id = @id", [new { id = DatabaseSeeder.Tenant1Owner.Id.ToString() }]
        );
        updatedLocale.Should().Be(locale);

        TelemetryEventsCollectorSpy.CollectedEvents.Count.Should().Be(1);
        TelemetryEventsCollectorSpy.CollectedEvents[0].GetType().Name.Should().Be("UserLocaleChanged");
        TelemetryEventsCollectorSpy.CollectedEvents[0].Properties["event.from_locale"].Should().Be(locale);
        TelemetryEventsCollectorSpy.CollectedEvents[0].Properties["event.to_locale"].Should().Be(locale);
        TelemetryEventsCollectorSpy.AreAllEventsDispatched.Should().BeTrue();
    }
}
