using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using PlatformPlatform.AccountManagement.Application.Tenants;
using PlatformPlatform.AccountManagement.Infrastructure;
using Xunit;

namespace PlatformPlatform.AccountManagement.Tests.Application.Tenants;

public sealed class CreateTenantValidationTests : BaseTest<AccountManagementDbContext>
{
    [Theory]
    [InlineData("Valid properties", "tenant2", "Tenant 2", "+44 (0)20 7946 0123", "test@test.com")]
    [InlineData("Valid properties - No phone", "tenant2", "Tenant 2", null, "test@test.com")]
    [InlineData("Valid properties - Empty phone", "tenant2", "Tenant 2", "", "test@test.com")]
    public async Task CreateTenant_WhenValidCommand_ShouldReturnSuccessfulResult(string scenario, string subdomain,
        string name, string phone, string email)
    {
        // Arrange
        var command = new CreateTenantCommand(subdomain, name, phone, email);
        var mediator = Provider.GetRequiredService<ISender>();

        // Act
        var result = await mediator.Send(command);

        // Assert
        result.IsSuccess.Should().BeTrue(scenario);
        result.Errors.Should().BeNull(scenario);
    }

    [Theory]
    [InlineData("Phone number too long", "tenant2", "Tenant 2", "123456789012345678901", "test@test.com")]
    [InlineData("Phone number invalid ", "tenant2", "Tenant 2", "+1 ### ###-INVALID", "test@test.com")]
    [InlineData("Tenant name empty", "", "Tenant 2", "1234567890", "test@test.com")]
    [InlineData("Tenant name too long ", "tenant2", "1234567890123456789012345678901", "1234567890", "test@test.com")]
    [InlineData("Email empty", "tenant2", "Tenant 2", "1234567890", "")]
    [InlineData("Email invalid", "tenant2", "Tenant 2", "1234567890", "@test.com")]
    [InlineData("Subdomain empty", "", "Tenant 2", "1234567890", "test@test.com")]
    [InlineData("Subdomain too short", "12", "Tenant 2", "1234567890", "test@test.com")]
    [InlineData("Subdomain too long", "1234567890123456789012345678901", "Tenant 2", "1234567890", "test@test.com")]
    [InlineData("Subdomain with uppercase", "Tenant2", "Tenant 2", "1234567890", "test@test.com")]
    [InlineData("Subdomain special characters", "tenant-2", "Tenant 2", "1234567890", "test@test.com")]
    [InlineData("Subdomain with spaces", "tenant 2", "Tenant 2", "1234567890", "test@test.com")]
    public async Task CreateTenant_WhenInvalidCommand_ShouldReturnUnsuccessfulResultWithOneError(string scenario,
        string subdomain, string name, string phone, string email)
    {
        // Arrange
        var command = new CreateTenantCommand(subdomain, name, phone, email);
        var mediator = Provider.GetRequiredService<ISender>();

        // Act
        var result = await mediator.Send(command);

        // Assert
        result.IsSuccess.Should().BeFalse(scenario);
        result.Errors?.Length.Should().Be(1, scenario);
    }
}