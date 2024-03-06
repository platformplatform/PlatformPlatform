using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using PlatformPlatform.AccountManagement.Application.Tenants;
using PlatformPlatform.AccountManagement.Infrastructure;
using Xunit;

namespace PlatformPlatform.AccountManagement.Tests.Application.Tenants;

public sealed class CreateTenantValidationTests : BaseTest<AccountManagementDbContext>
{
    [Fact]
    public async Task CreateTenant_WhenValidCommand_ShouldReturnSuccessfulResult()
    {
        // Arrange
        var command = new CreateTenantCommand(DatabaseSeeder.AccountRegistration1.Id);
        var mediator = Provider.GetRequiredService<ISender>();

        // Act
        var result = await mediator.Send(command);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Errors.Should().BeNull();

        TelemetryEventsCollectorSpy.CollectedEvents.Count.Should().Be(2);

        TelemetryEventsCollectorSpy.CollectedEvents.Count(e =>
            e.Name == "TenantCreated" &&
            e.Properties["Event_TenantId"] == DatabaseSeeder.AccountRegistration1.TenantId &&
            e.Properties["Event_TenantState"] == "Trial"
        ).Should().Be(1);

        TelemetryEventsCollectorSpy.CollectedEvents.Count(e =>
            e.Name == "UserCreated" &&
            e.Properties["Event_TenantId"] == DatabaseSeeder.AccountRegistration1.TenantId
        ).Should().Be(1);
    }

    [Theory]
    [InlineData("Tenant name empty", "", "Tenant 2")]
    [InlineData("Tenant name too long ", "tenant2", "1234567890123456789012345678901")]
    [InlineData("Subdomain empty", "", "Tenant 2")]
    [InlineData("Subdomain too short", "12", "Tenant 2")]
    [InlineData("Subdomain too long", "1234567890123456789012345678901", "Tenant 2")]
    [InlineData("Subdomain with uppercase", "Tenant2", "Tenant 2")]
    [InlineData("Subdomain special characters", "tenant-2", "Tenant 2")]
    [InlineData("Subdomain with spaces", "tenant 2", "Tenant 2")]
    public async Task CreateTenant_WhenInvalidCommand_ShouldReturnUnsuccessfulResultWithOneError(
        string scenario,
        string subdomain,
        string name
    )
    {
        // Arrange
        var command = new CreateTenantCommand(DatabaseSeeder.AccountRegistration1.Id);
        var mediator = Provider.GetRequiredService<ISender>();

        // Act
        var result = await mediator.Send(command);

        // Assert
        result.IsSuccess.Should().BeFalse(scenario);
        result.Errors?.Length.Should().Be(1, scenario);
    }
}