using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using PlatformPlatform.AccountManagement.Application;
using PlatformPlatform.AccountManagement.Application.Tenants.Commands;
using PlatformPlatform.AccountManagement.Domain.Tenants;
using Xunit;

namespace PlatformPlatform.AccountManagement.Tests.Application.Tenants.Commands;

public class UpdateTenantCommandTests
{
    private readonly ITenantRepository _tenantRepository;

    public UpdateTenantCommandTests()
    {
        var services = new ServiceCollection();
        services.AddApplicationServices();

        _tenantRepository = Substitute.For<ITenantRepository>();
    }

    [Fact]
    public async Task UpdateTenantCommandHandler_WhenCommandIsValid_ShouldUpdateTenantInRepository()
    {
        // Arrange
        var existingTenant = Tenant.Create("ExistingTenant", "tenant1", "foo@tenant1.com", "1234567890");
        var existingTenantId = existingTenant.Id;
        _tenantRepository.GetByIdAsync(existingTenantId, Arg.Any<CancellationToken>()).Returns(existingTenant);
        var handler = new UpdateTenantCommandHandler(_tenantRepository);

        // Act
        var command = new UpdateTenantCommand(existingTenantId, "UpdatedTenant", "bar@tenant1.com", "0987654321");
        var updateTenantCommandResult = await handler.Handle(command, CancellationToken.None);

        // Assert
        updateTenantCommandResult.IsSuccess.Should().BeTrue();
        var updatedTenant = updateTenantCommandResult.Value!;
        updatedTenant.Name.Should().Be(command.Name);
        updatedTenant.Email.Should().Be(command.Email);
        updatedTenant.Phone.Should().Be(command.Phone);
    }

    [Fact]
    public async Task UpdateTenantCommandHandler_WhenTenantDoesNotExist_ShouldReturnFailure()
    {
        // Arrange
        var nonExistingTenantId = TenantId.NewId();
        _tenantRepository.GetByIdAsync(nonExistingTenantId, Arg.Any<CancellationToken>()).Returns(null as Tenant);
        var handler = new UpdateTenantCommandHandler(_tenantRepository);

        // Act
        var command = new UpdateTenantCommand(nonExistingTenantId, "UpdatedTenant", "bar@tenant1.com", "0987654321");
        var updateTenantCommandResult = await handler.Handle(command, CancellationToken.None);

        // Assert
        updateTenantCommandResult.IsSuccess.Should().BeFalse();
    }
}