using FluentAssertions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PlatformPlatform.AccountManagement.Application.Tenants;
using PlatformPlatform.AccountManagement.Infrastructure;
using Xunit;

namespace PlatformPlatform.AccountManagement.Tests.Application.Tenants;

public sealed class CreateTenantTests : BaseTest<AccountManagementDbContext>
{
    [Fact]
    public async Task CreateTenantHandler_WhenCommandIsValid_ShouldAddTenantToRepository()
    {
        // Arrange
        var mediator = Provider.GetRequiredService<IMediator>();

        // Act
        var command = new CreateTenant.Command("tenant1", "TestTenant", "1234567890", "test@test.com");
        var result = await mediator.Send(command);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var tenantId = result.Value!;

        // Query the database to find the added tenant
        var dbContext = Provider.GetRequiredService<AccountManagementDbContext>();
        var tenant = await dbContext.Tenants.SingleOrDefaultAsync(t => t.Id == tenantId);

        // Check that the tenant exists and has the expected properties
        tenant.Should().NotBeNull();
        tenant!.Id.Should().Be(tenantId);
        tenant.Name.Should().Be(command.Name);
        tenant.Phone.Should().Be(command.Phone);
    }

    [Theory]
    [InlineData("Valid properties", "tenant1", "test@test.com", "+44 (0)20 7946 0123")]
    [InlineData("No phone number (valid)", "tenant1", "test@test.com", null)]
    [InlineData("Empty phone number", "tenant1", "test@test.com", "")]
    public async Task CreateTenantHandler_WhenValidCommand_ShouldReturnSuccessfulResult(string name, string subdomain,
        string email, string phone)
    {
        // Arrange
        var command = new CreateTenant.Command(subdomain, name, phone, email);

        var mediator = Provider.GetRequiredService<IMediator>();

        // Act
        var result = await mediator.Send(command);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Errors.Should().BeNull();
    }

    [Theory]
    [InlineData("To long phone number", "tenant1", "test@test.com", "0099 (999) 888-77-66-55")]
    [InlineData("Invalid phone number", "tenant1", "test@test.com", "N/A")]
    [InlineData("", "notenantname", "test@test.com", "1234567890")]
    [InlineData("Too long tenant name above 30 characters", "tenant1", "test@test.com", "+55 (21) 99999-9999")]
    [InlineData("No email", "tenant1", "", "+61 2 1234 5678")]
    [InlineData("Invalid Email", "tenant1", "@test.com", "1234567890")]
    [InlineData("No subdomain", "", "test@test.com", "1234567890")]
    [InlineData("To short subdomain", "ab", "test@test.com", "1234567890")]
    [InlineData("To long subdomain", "1234567890123456789012345678901", "test@test.com", "1234567890")]
    [InlineData("Subdomain with uppercase", "Tenant1", "test@test.com", "1234567890")]
    [InlineData("Subdomain special characters", "tenant-1", "test@test.com", "1234567890")]
    [InlineData("Subdomain with spaces", "tenant 1", "test@test.com", "1234567890")]
    public async Task CreateTenantHandler_WhenInvalidCommand_ShouldReturnUnsuccessfulResultWithOneError(string name,
        string subdomain, string email, string phone)
    {
        // Arrange
        var command = new CreateTenant.Command(subdomain, name, phone, email);

        var mediator = Provider.GetRequiredService<IMediator>();

        // Act
        var result = await mediator.Send(command);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors?.Length.Should().Be(1);
    }
}