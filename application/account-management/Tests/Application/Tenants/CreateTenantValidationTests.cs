using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using PlatformPlatform.AccountManagement.Application.Tenants;
using PlatformPlatform.AccountManagement.Infrastructure;
using Xunit;

namespace PlatformPlatform.AccountManagement.Tests.Application.Tenants;

public sealed class CreateTenantValidationTests : BaseTest<AccountManagementDbContext>
{
    [Theory]
    [InlineData("Valid properties", "tenant2", "Tenant 2", "+44 (0)20 7946 0123")]
    [InlineData("Valid properties - No phone", "tenant2", "Tenant 2", null)]
    [InlineData("Valid properties - Empty phone", "tenant2", "Tenant 2", "")]
    public async Task CreateTenant_WhenValidCommand_ShouldReturnSuccessfulResult(
        string scenario,
        string subdomain,
        string name,
        string? phone
    )
    {
        // Arrange
        var command = new CreateTenantCommand(DatabaseSeeder.AccountRegistration1.Id, subdomain, name, phone);
        var mediator = Provider.GetRequiredService<ISender>();

        // Act
        var result = await mediator.Send(command);

        // Assert
        result.IsSuccess.Should().BeTrue(scenario);
        result.Errors.Should().BeNull(scenario);

        TelemetryEventsCollectorSpy.CollectedEvents.Count.Should().Be(2);

        TelemetryEventsCollectorSpy.CollectedEvents.Count(e =>
            e.Name == "TenantCreated" &&
            e.Properties["Event_TenantId"] == subdomain &&
            e.Properties["Event_TenantState"] == "Trial"
        ).Should().Be(1);

        TelemetryEventsCollectorSpy.CollectedEvents.Count(e =>
            e.Name == "UserCreated" &&
            e.Properties["Event_TenantId"] == subdomain
        ).Should().Be(1);
    }

    [Theory]
    [InlineData("Phone number too long", "tenant2", "Tenant 2", "123456789012345678901")]
    [InlineData("Phone number invalid ", "tenant2", "Tenant 2", "+1 ### ###-INVALID")]
    [InlineData("Tenant name empty", "", "Tenant 2", "1234567890")]
    [InlineData("Tenant name too long ", "tenant2", "1234567890123456789012345678901", "1234567890")]
    [InlineData("Subdomain empty", "", "Tenant 2", "1234567890")]
    [InlineData("Subdomain too short", "12", "Tenant 2", "1234567890")]
    [InlineData("Subdomain too long", "1234567890123456789012345678901", "Tenant 2", "1234567890")]
    [InlineData("Subdomain with uppercase", "Tenant2", "Tenant 2", "1234567890")]
    [InlineData("Subdomain special characters", "tenant-2", "Tenant 2", "1234567890")]
    [InlineData("Subdomain with spaces", "tenant 2", "Tenant 2", "1234567890")]
    public async Task CreateTenant_WhenInvalidCommand_ShouldReturnUnsuccessfulResultWithOneError(
        string scenario,
        string subdomain,
        string name,
        string phone
    )
    {
        // Arrange
        var command = new CreateTenantCommand(DatabaseSeeder.AccountRegistration1.Id, subdomain, name, phone);
        var mediator = Provider.GetRequiredService<ISender>();

        // Act
        var result = await mediator.Send(command);

        // Assert
        result.IsSuccess.Should().BeFalse(scenario);
        result.Errors?.Length.Should().Be(1, scenario);
    }
}