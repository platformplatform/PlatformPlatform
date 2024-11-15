using Bogus;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using PlatformPlatform.SharedKernel.Authentication;
using PlatformPlatform.SharedKernel.ExecutionContext;
using PlatformPlatform.SharedKernel.Integrations.Email;
using PlatformPlatform.SharedKernel.Telemetry;
using PlatformPlatform.SharedKernel.Tests.Telemetry;

namespace PlatformPlatform.BackOffice.Tests;

public abstract class BaseTest<TContext> : IDisposable where TContext : DbContext
{
    protected readonly AccessTokenGenerator AccessTokenGenerator;
    protected readonly IEmailClient EmailClient;
    protected readonly Faker Faker = new();
    protected readonly ServiceCollection Services;
    private ServiceProvider? _provider;
    protected TelemetryEventsCollectorSpy TelemetryEventsCollectorSpy;

    protected BaseTest()
    {
        Environment.SetEnvironmentVariable(
            "APPLICATIONINSIGHTS_CONNECTION_STRING",
            "InstrumentationKey=00000000-0000-0000-0000-000000000000;IngestionEndpoint=https://localhost;LiveEndpoint=https://localhost"
        );

        Services = new ServiceCollection();

        Services.AddLogging();
        Services.AddTransient<DatabaseSeeder>();

        // Create connection and add DbContext to the service collection
        Connection = new SqliteConnection("DataSource=:memory:");
        Connection.Open();
        Services.AddDbContext<TContext>(options => { options.UseSqlite(Connection); });

        Services.AddBackOfficeServices();

        TelemetryEventsCollectorSpy = new TelemetryEventsCollectorSpy(new TelemetryEventsCollector());
        Services.AddScoped<ITelemetryEventsCollector>(_ => TelemetryEventsCollectorSpy);

        EmailClient = Substitute.For<IEmailClient>();
        Services.AddScoped<IEmailClient>(_ => EmailClient);

        var telemetryChannel = Substitute.For<ITelemetryChannel>();
        Services.AddSingleton(new TelemetryClient(new TelemetryConfiguration { TelemetryChannel = telemetryChannel }));

        Services.AddScoped<IExecutionContext, HttpExecutionContext>();

        // Build the ServiceProvider
        _provider = Services.BuildServiceProvider();

        // Make sure the database is created
        using var serviceScope = Provider.CreateScope();
        serviceScope.ServiceProvider.GetRequiredService<TContext>().Database.EnsureCreated();
        DatabaseSeeder = serviceScope.ServiceProvider.GetRequiredService<DatabaseSeeder>();

        AccessTokenGenerator = serviceScope.ServiceProvider.GetRequiredService<AccessTokenGenerator>();
    }

    protected SqliteConnection Connection { get; }

    protected DatabaseSeeder DatabaseSeeder { get; }

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
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposing) return;
        Provider.Dispose();
        Connection.Close();
    }
}
