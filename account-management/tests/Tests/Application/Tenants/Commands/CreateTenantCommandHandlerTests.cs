using NSubstitute;
using PlatformPlatform.AccountManagement.Application.Tenants.Commands;
using PlatformPlatform.AccountManagement.Domain.Tenants;
using Xunit;

namespace PlatformPlatform.AccountManagement.Tests.Application.Tenants.commands;

public class CreateTenantCommandHandlerTests
{
    [Fact]
    public async Task CreateTenantCommandHandler_ShouldAddTenantToRepository()
    {
        // Arrange
        var startId = TenantId.NewId(); // NewId will always generate an id that are greater than the previous one
        var tenantRepository = Substitute.For<ITenantRepository>();
        var handler = new CreateTenantCommandHandler(tenantRepository);

        // Act
        var command = new CreateTenantCommand("TestTenant", "tenant1", "foo@tenant1.com", "1234567890");
        var tenantResponseDto = await handler.Handle(command, CancellationToken.None);

        // Assert
        var tenantId = TenantId.FromString(tenantResponseDto.Id);
        await tenantRepository.Received()
            .AddAsync(Arg.Is<Tenant>(t => t.Name == command.Name && t.Id > startId && t.Id == tenantId),
                Arg.Any<CancellationToken>());
    }
}