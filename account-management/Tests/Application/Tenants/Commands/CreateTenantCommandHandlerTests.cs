using NSubstitute;
using PlatformPlatform.AccountManagement.Application.Tenants.Commands;
using PlatformPlatform.AccountManagement.Domain.Tenants;

namespace PlatformPlatform.AccountManagement.Tests.Application.Tenants.commands;

public class CreateTenantCommandHandlerTests
{
    [Fact]
    public async Task CreateTenantCommandHandler_ShouldAddTenantToRepository()
    {
        // Arrange
        var startId = IdGenerator.NewId(); // NewId will always generate an id that are greater than the previous one

        var tenantRepository = Substitute.For<ITenantRepository>();

        var command = new CreateTenantCommand("TestTenant");
        var handler = new CreateTenantCommandHandler(tenantRepository);

        // Act
        var tenantId = await handler.Handle(command, CancellationToken.None);

        // Assert
        await tenantRepository.Received()
            .AddAsync(Arg.Is<Tenant>(t => t.Name == command.Name && t.Id > startId && t.Id == tenantId),
                Arg.Any<CancellationToken>());
    }
}