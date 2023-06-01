using MediatR;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using PlatformPlatform.AccountManagement.Application;
using PlatformPlatform.AccountManagement.Application.Tenants;
using PlatformPlatform.AccountManagement.Infrastructure;
using Xunit;

namespace PlatformPlatform.AccountManagement.Tests.Application.Tenants;

public sealed class TenantCreatedEventHandlerTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly IMediator _mediator;
    private readonly ILogger<TenantCreatedEventHandler> _mockLogger;
    private readonly ServiceProvider _provider;

    public TenantCreatedEventHandlerTests()
    {
        var services = new ServiceCollection();

        services.AddLogging();

        // Replace the DbContext with an in-memory SQLite version
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        services.AddDbContext<AccountManagementDbContext>(options => { options.UseSqlite(_connection); });
        using (var scope = services.BuildServiceProvider().CreateScope())
        {
            scope.ServiceProvider.GetRequiredService<AccountManagementDbContext>().Database.EnsureCreated();
        }

        var configuration = new ConfigurationBuilder().AddEnvironmentVariables().Build();
        services
            .AddApplicationServices()
            .AddInfrastructureServices(configuration);

        _mockLogger = Substitute.For<ILogger<TenantCreatedEventHandler>>();
        services.AddSingleton(_mockLogger);

        _provider = services.BuildServiceProvider();
        _mediator = _provider.GetRequiredService<IMediator>();
    }

    public void Dispose()
    {
        _connection.Close();
        _provider.Dispose();
    }

    [Fact]
    public async Task TenantCreatedEvent_WhenTenantIsCreated_ShouldLogCorrectInformation()
    {
        // Arrange
        var cancellationToken = new CancellationToken();
        var command = new CreateTenant.Command("TestTenant", "tenant1", "test@test.com", "1234567890");

        // Act
        var _ = await _mediator.Send(command, cancellationToken);

        // Assert
        _mockLogger.Received().LogInformation("Raise event to send Welcome mail to tenant");
    }
}