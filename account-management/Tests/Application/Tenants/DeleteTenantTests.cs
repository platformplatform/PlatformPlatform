using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using PlatformPlatform.AccountManagement.Application;
using PlatformPlatform.AccountManagement.Application.Tenants;
using PlatformPlatform.AccountManagement.Domain.Tenants;
using Xunit;

namespace PlatformPlatform.AccountManagement.Tests.Application.Tenants;

public class DeleteTenantTests
{
    private readonly ITenantRepository _tenantRepository;

    public DeleteTenantTests()
    {
        var services = new ServiceCollection();
        services.AddApplicationServices();

        _tenantRepository = Substitute.For<ITenantRepository>();
    }

    [Fact]
    public async Task DeleteTenantHandler_WhenTenantExists_ShouldDeleteTenantFromRepository()
    {
        // Arrange
        var existingTenant = Tenant.Create("ExistingTenant", "tenant1", "test@test.com", "1234567890");
        var existingTenantId = existingTenant.Id;
        _tenantRepository.GetByIdAsync(existingTenantId, Arg.Any<CancellationToken>()).Returns(existingTenant);
        var handler = new DeleteTenant.Handler(_tenantRepository);

        // Act
        var command = new DeleteTenant.Command(existingTenantId);
        var deleteTenantCommandResult = await handler.Handle(command, CancellationToken.None);

        // Assert
        deleteTenantCommandResult.IsSuccess.Should().BeTrue();
        _tenantRepository.Received().Remove(existingTenant);
    }

    [Fact]
    public async Task DeleteTenantHandler_WhenTenantDoesNotExist_ShouldReturnFailure()
    {
        // Arrange
        var nonExistingTenantId = new TenantId("unknown");
        _tenantRepository.GetByIdAsync(nonExistingTenantId, Arg.Any<CancellationToken>()).Returns(null as Tenant);
        var handler = new DeleteTenant.Handler(_tenantRepository);

        // Act
        var command = new DeleteTenant.Command(nonExistingTenantId);
        var deleteTenantCommandResult = await handler.Handle(command, CancellationToken.None);

        // Assert
        deleteTenantCommandResult.IsSuccess.Should().BeFalse();
    }
}