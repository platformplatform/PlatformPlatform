using FluentAssertions;
using MediatR;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using PlatformPlatform.AccountManagement.Application;
using PlatformPlatform.AccountManagement.Application.Tenants;
using PlatformPlatform.AccountManagement.Domain.Tenants;
using PlatformPlatform.AccountManagement.Infrastructure;
using Xunit;

namespace PlatformPlatform.AccountManagement.Tests.Application.Tenants;

public sealed class CreateTenantTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly IMediator _mediator;
    private readonly ServiceProvider _provider;

    public CreateTenantTests()
    {
        var services = new ServiceCollection();

        services.AddLogging();

        // Replace the DbContext with an in-memory SQLite version
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        services.RemoveAll(typeof(AccountManagementDbContext));
        services.AddDbContext<AccountManagementDbContext>(options => { options.UseSqlite(_connection); });
        using (var scope = services.BuildServiceProvider().CreateScope())
        {
            scope.ServiceProvider.GetRequiredService<AccountManagementDbContext>().Database.EnsureCreated();
        }

        var configuration = new ConfigurationBuilder().AddEnvironmentVariables().Build();
        services
            .AddApplicationServices()
            .AddInfrastructureServices(configuration);

        _provider = services.BuildServiceProvider();
        _mediator = _provider.GetRequiredService<IMediator>();
        _provider.GetRequiredService<ITenantRepository>();
    }

    public void Dispose()
    {
        _connection.Close();
        _provider.Dispose();
    }

    [Fact]
    public async Task CreateTenantHandler_WhenCommandIsValid_ShouldAddTenantToRepository()
    {
        // Arrange
        var cancellationToken = new CancellationToken();

        // Act
        var command = new CreateTenant.Command("TestTenant", "tenant1", "test@test.com", "1234567890");
        var result = await _mediator.Send(command, cancellationToken);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var tenantId = result.Value!;

        // Query the database to find the added tenant
        var dbContext = _provider.GetRequiredService<AccountManagementDbContext>();
        var tenant = await dbContext.Tenants.SingleOrDefaultAsync(t => t.Id == tenantId, cancellationToken);

        // Check that the tenant exists and has the expected properties
        tenant.Should().NotBeNull();
        tenant!.Id.Should().Be(tenantId);
        tenant.Subdomain.Should().Be(command.Subdomain);
        tenant.Name.Should().Be(command.Name);
        tenant.Email.Should().Be(command.Email);
        tenant.Phone.Should().Be(command.Phone);
    }

    [Theory]
    [InlineData("Valid properties", "tenant1", "test@test.com", "+44 (0)20 7946 0123", true)]
    [InlineData("No phone number (valid)", "tenant1", "test@test.com", null, true)]
    [InlineData("Empty phone number", "tenant1", "test@test.com", "", true)]
    [InlineData("To long phone number", "tenant1", "test@test.com", "0099 (999) 888-77-66-55", false)]
    [InlineData("Invalid phone number", "tenant1", "test@test.com", "N/A", false)]
    [InlineData("", "notenantname", "test@test.com", "1234567890", false)]
    [InlineData("Too long tenant name above 30 characters", "tenant1", "test@test.com", "+55 (21) 99999-9999", false)]
    [InlineData("No email", "tenant1", "", "+61 2 1234 5678", false)]
    [InlineData("Invalid Email", "tenant1", "@test.com", "1234567890", false)]
    [InlineData("No subdomain", "", "test@test.com", "1234567890", false)]
    [InlineData("To short subdomain", "ab", "test@test.com", "1234567890", false)]
    [InlineData("To long subdomain", "1234567890123456789012345678901", "test@test.com", "1234567890", false)]
    [InlineData("Subdomain with uppercase", "Tenant1", "test@test.com", "1234567890", false)]
    [InlineData("Subdomain special characters", "tenant-1", "test@test.com", "1234567890", false)]
    [InlineData("Subdomain with spaces", "tenant 1", "test@test.com", "1234567890", false)]
    public async Task CreateTenantHandler_WhenValidatingCommand_ShouldValidateCorrectly(string name,
        string subdomain, string email,
        string phone, bool expected)
    {
        // Arrange
        var command = new CreateTenant.Command(name, subdomain, email, phone);

        // Act
        var result = await _mediator.Send(command);

        // Assert
        result.IsSuccess.Should().Be(expected);
        result.Errors?.Length.Should().Be(expected ? null : 1);
    }
}