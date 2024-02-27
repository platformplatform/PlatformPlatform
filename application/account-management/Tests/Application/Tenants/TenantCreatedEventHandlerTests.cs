using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using PlatformPlatform.AccountManagement.Application.Tenants;
using PlatformPlatform.AccountManagement.Infrastructure;
using Xunit;

namespace PlatformPlatform.AccountManagement.Tests.Application.Tenants;

public sealed class TenantCreatedEventHandlerTests : BaseTest<AccountManagementDbContext>
{
    [Fact]
    public async Task TenantCreatedEvent_WhenTenantIsCreated_ShouldLogCorrectInformation()
    {
        // Arrange
        var mockLogger = Substitute.For<ILogger<TenantCreatedEventHandler>>();
        Services.AddSingleton(mockLogger);
        var mediator = Provider.GetRequiredService<ISender>();

        // Act
        var command = new CreateTenantCommand(DatabaseSeeder.AccountRegistration1.Id, "tenant2", "TestTenant");
        _ = await mediator.Send(command);

        // Assert
        mockLogger.Received().LogInformation("Raise event to send Welcome mail to tenant.");
    }
}