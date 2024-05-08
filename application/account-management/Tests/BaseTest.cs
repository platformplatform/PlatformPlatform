using System.Text.Json;
using Bogus;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NSubstitute;
using PlatformPlatform.AccountManagement.Application;
using PlatformPlatform.AccountManagement.Infrastructure;
using PlatformPlatform.SharedKernel.ApplicationCore.Services;
using PlatformPlatform.SharedKernel.ApplicationCore.TelemetryEvents;
using PlatformPlatform.SharedKernel.Tests.ApplicationCore.TelemetryEvents;

namespace PlatformPlatform.AccountManagement.Tests;

public abstract class BaseTest<TContext> : IDisposable where TContext : DbContext
{
    protected readonly IEmailService EmailService;
    protected readonly Faker Faker = new();
    protected readonly JsonSerializerOptions JsonSerializerOptions;
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
        Services.AddDbContextPool<TContext>(options => { options.UseSqlite(Connection); });
        
        Services
            .AddApplicationServices()
            .AddInfrastructureServices();
        
        TelemetryEventsCollectorSpy = new TelemetryEventsCollectorSpy(new TelemetryEventsCollector());
        Services.AddScoped<ITelemetryEventsCollector>(_ => TelemetryEventsCollectorSpy);
        
        EmailService = Substitute.For<IEmailService>();
        Services.AddScoped<IEmailService>(_ => EmailService);
        
        var telemetryChannel = Substitute.For<ITelemetryChannel>();
        Services.AddSingleton(new TelemetryClient(new TelemetryConfiguration { TelemetryChannel = telemetryChannel }));
        
        // Make sure database is created
        using var serviceScope = Services.BuildServiceProvider().CreateScope();
        serviceScope.ServiceProvider.GetRequiredService<TContext>().Database.EnsureCreated();
        DatabaseSeeder = serviceScope.ServiceProvider.GetRequiredService<DatabaseSeeder>();
        
        JsonSerializerOptions = serviceScope.ServiceProvider.GetRequiredService<IOptions<JsonOptions>>().Value.SerializerOptions;
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
