using FluentAssertions;
using NSubstitute;
using PlatformPlatform.AccountManagement.Application.Tenants;
using PlatformPlatform.AccountManagement.Domain.Tenants;
using PlatformPlatform.AccountManagement.Infrastructure;
using Xunit;

namespace PlatformPlatform.AccountManagement.Tests.Application.Tenants;

public class DeleteTenantTests : BaseTest<AccountManagementDbContext>
{
    [Fact]
    public async Task DeleteTenantHandler_WhenTenantExists_ShouldDeleteTenantFromRepository()
    {
        // Arrange
        var existingTenant = Tenant.Create("tenant1", "ExistingTenant", "1234567890");
        var existingTenantId = existingTenant.Id;

        var tenantRepository = Substitute.For<ITenantRepository>();
        tenantRepository.GetByIdAsync(existingTenantId, Arg.Any<CancellationToken>()).Returns(existingTenant);
        var handler = new DeleteTenant.Handler(tenantRepository);

        // Act
        var command = new DeleteTenant.Command(existingTenantId);
        var deleteTenantCommandResult = await handler.Handle(command, CancellationToken.None);

        // Assert
        deleteTenantCommandResult.IsSuccess.Should().BeTrue();
        tenantRepository.Received().Remove(existingTenant);
    }

    [Fact]
    public async Task DeleteTenantHandler_WhenTenantDoesNotExist_ShouldReturnFailure()
    {
        // Arrange
        var nonExistingTenantId = new TenantId("unknown");

        var tenantRepository = Substitute.For<ITenantRepository>();
        tenantRepository.GetByIdAsync(nonExistingTenantId, Arg.Any<CancellationToken>()).Returns(null as Tenant);
        var handler = new DeleteTenant.Handler(tenantRepository);

        // Act
        var command = new DeleteTenant.Command(nonExistingTenantId);
        var deleteTenantCommandResult = await handler.Handle(command, CancellationToken.None);

        // Assert
        deleteTenantCommandResult.IsSuccess.Should().BeFalse();
    }
}