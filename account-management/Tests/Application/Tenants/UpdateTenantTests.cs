using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using PlatformPlatform.AccountManagement.Application;
using PlatformPlatform.AccountManagement.Application.Tenants;
using PlatformPlatform.AccountManagement.Domain.Tenants;
using Xunit;

namespace PlatformPlatform.AccountManagement.Tests.Application.Tenants;

public class UpdateTenantTests
{
    private readonly ITenantRepository _tenantRepository;

    public UpdateTenantTests()
    {
        var services = new ServiceCollection();
        services.AddApplicationServices();

        _tenantRepository = Substitute.For<ITenantRepository>();
    }

    [Fact]
    public async Task UpdateTenantHandler_WhenCommandIsValid_ShouldUpdateTenantInRepository()
    {
        // Arrange
        var existingTenant = Tenant.Create("tenant1", "ExistingTenant", "1234567890");
        var existingTenantId = existingTenant.Id;
        _tenantRepository.GetByIdAsync(existingTenantId, Arg.Any<CancellationToken>()).Returns(existingTenant);
        var handler = new UpdateTenant.Handler(_tenantRepository);

        // Act
        var command = new UpdateTenant.Command {Id = existingTenantId, Name = "UpdatedTenant", Phone = "0987654321"};
        var updateTenantCommandResult = await handler.Handle(command, CancellationToken.None);

        // Assert
        updateTenantCommandResult.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateTenantHandler_WhenTenantDoesNotExist_ShouldReturnFailure()
    {
        // Arrange
        var nonExistingTenantId = new TenantId("unknown");
        _tenantRepository.GetByIdAsync(nonExistingTenantId, Arg.Any<CancellationToken>()).Returns(null as Tenant);
        var handler = new UpdateTenant.Handler(_tenantRepository);

        // Act
        var command = new UpdateTenant.Command {Id = nonExistingTenantId, Name = "UpdatedTenant", Phone = "0987654321"};
        var updateTenantCommandResult = await handler.Handle(command, CancellationToken.None);

        // Assert
        updateTenantCommandResult.IsSuccess.Should().BeFalse();
    }
}