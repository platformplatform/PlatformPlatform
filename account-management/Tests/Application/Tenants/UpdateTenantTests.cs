using FluentAssertions;
using NSubstitute;
using PlatformPlatform.AccountManagement.Application.Tenants;
using PlatformPlatform.AccountManagement.Domain.Tenants;
using PlatformPlatform.AccountManagement.Infrastructure;
using Xunit;

namespace PlatformPlatform.AccountManagement.Tests.Application.Tenants;

public class UpdateTenantTests : BaseTest<AccountManagementDbContext>
{
    [Fact]
    public async Task UpdateTenantHandler_WhenCommandIsValid_ShouldUpdateTenantInRepository()
    {
        // Arrange
        var existingTenant = Tenant.Create("tenant1", "ExistingTenant", "1234567890");
        var existingTenantId = existingTenant.Id;

        var tenantRepository = Substitute.For<ITenantRepository>();
        tenantRepository.GetByIdAsync(existingTenantId, Arg.Any<CancellationToken>()).Returns(existingTenant);
        var handler = new UpdateTenant.Handler(tenantRepository);

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

        var tenantRepository = Substitute.For<ITenantRepository>();
        tenantRepository.GetByIdAsync(nonExistingTenantId, Arg.Any<CancellationToken>()).Returns(null as Tenant);
        var handler = new UpdateTenant.Handler(tenantRepository);

        // Act
        var command = new UpdateTenant.Command {Id = nonExistingTenantId, Name = "UpdatedTenant", Phone = "0987654321"};
        var updateTenantCommandResult = await handler.Handle(command, CancellationToken.None);

        // Assert
        updateTenantCommandResult.IsSuccess.Should().BeFalse();
    }
}