using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PlatformPlatform.AccountManagement.Application;
using PlatformPlatform.AccountManagement.Infrastructure;

namespace PlatformPlatform.AccountManagement.Tests;

public abstract class BaseTest<TContext> : IDisposable where TContext : DbContext
{
    protected readonly ServiceCollection Services;
    private ServiceProvider? _provider;

    protected BaseTest()
    {
        Services = new ServiceCollection();

        Services.AddLogging();

        // Create connection and add DbContext to the service collection
        Connection = new SqliteConnection("DataSource=:memory:");
        Connection.Open();
        Services.AddDbContext<TContext>(options => { options.UseSqlite(Connection); });

        // Make sure database is created
        using (var scope = Services.BuildServiceProvider().CreateScope())
        {
            scope.ServiceProvider.GetRequiredService<TContext>().Database.EnsureCreated();
        }

        var configuration = new ConfigurationBuilder().AddEnvironmentVariables().Build();
        Services
            .AddApplicationServices()
            .AddInfrastructureServices(configuration);
    }

    protected SqliteConnection Connection { get; }

    protected ServiceProvider Provider
    {
        get
        {
            // ServiceProvider is created on first access to allow Tests to configure services in the constructor
            // before the ServiceProvider is created
            return _provider ??= Services.BuildServiceProvider();
        }
    }

    public void Dispose()
    {
        Connection.Close();
        Provider.Dispose();
        GC.SuppressFinalize(this);
    }

    protected virtual void ConfigureServices(IServiceCollection services)
    {
    }
}