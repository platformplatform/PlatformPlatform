using System.Net.Http.Headers;
using Bogus;
using JetBrains.Annotations;
using Mapster;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using PlatformPlatform.SharedKernel.Authentication;
using PlatformPlatform.SharedKernel.Authentication.TokenGeneration;
using PlatformPlatform.SharedKernel.ExecutionContext;
using PlatformPlatform.SharedKernel.Integrations.Email;
using PlatformPlatform.SharedKernel.SinglePageApp;
using PlatformPlatform.SharedKernel.Telemetry;
using PlatformPlatform.SharedKernel.Tests.Telemetry;

namespace PlatformPlatform.AccountManagement.Tests;

public abstract class EndpointBaseTest<TContext> : IDisposable where TContext : DbContext
{
    private readonly WebApplicationFactory<Program> _webApplicationFactory;
    protected readonly AccessTokenGenerator AccessTokenGenerator;
    protected readonly IEmailClient EmailClient;
    protected readonly Faker Faker = new();
    protected readonly ServiceCollection Services;
    private ServiceProvider? _provider;
    protected TelemetryEventsCollectorSpy TelemetryEventsCollectorSpy;

    protected EndpointBaseTest()
    {
        Environment.SetEnvironmentVariable(SinglePageAppConfiguration.PublicUrlKey, "https://localhost:9000");
        Environment.SetEnvironmentVariable(SinglePageAppConfiguration.CdnUrlKey, "https://localhost:9000/account-management");
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

        Services.AddAccountManagementServices();

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

        _webApplicationFactory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                    {
                        // Replace the default DbContext in the WebApplication to use an in-memory SQLite database
                        services.Remove(services.Single(d => d.ServiceType == typeof(IDbContextOptionsConfiguration<TContext>)));
                        services.AddDbContext<TContext>(options => { options.UseSqlite(Connection); });

                        TelemetryEventsCollectorSpy = new TelemetryEventsCollectorSpy(new TelemetryEventsCollector());
                        services.AddScoped<ITelemetryEventsCollector>(_ => TelemetryEventsCollectorSpy);

                        services.Remove(services.Single(d => d.ServiceType == typeof(IEmailClient)));
                        services.AddTransient<IEmailClient>(_ => EmailClient);

                        RegisterMockLoggers(services);

                        services.AddScoped<IExecutionContext, HttpExecutionContext>();
                    }
                );
            }
        );

        AnonymousHttpClient = _webApplicationFactory.CreateClient();

        var ownerAccessToken = AccessTokenGenerator.Generate(DatabaseSeeder.Tenant1Owner.Adapt<UserInfo>());
        AuthenticatedOwnerHttpClient = _webApplicationFactory.CreateClient();
        AuthenticatedOwnerHttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ownerAccessToken);

        var memberAccessToken = AccessTokenGenerator.Generate(DatabaseSeeder.Tenant1Member.Adapt<UserInfo>());
        AuthenticatedMemberHttpClient = _webApplicationFactory.CreateClient();
        AuthenticatedMemberHttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", memberAccessToken);

        // Set the environment variable to bypass antiforgery validation on the server. ASP.NET uses a cryptographic
        // double-submit pattern that encrypts the user's ClaimUid in the token, which is complex to replicate in tests
        Environment.SetEnvironmentVariable("BypassAntiforgeryValidation", "true");
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

    protected HttpClient AnonymousHttpClient { get; }

    protected HttpClient AuthenticatedOwnerHttpClient { get; }

    protected HttpClient AuthenticatedMemberHttpClient { get; }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void RegisterMockLoggers(IServiceCollection services)
    {
    }

    // SonarLint complains if the virtual keyword is missing, as it is required to correctly implement the dispose pattern.
    [UsedImplicitly]
    protected virtual void Dispose(bool disposing)
    {
        if (!disposing) return;
        Provider.Dispose();
        Connection.Close();
        _webApplicationFactory.Dispose();
    }
}
