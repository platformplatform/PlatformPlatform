using System.Net.Http.Headers;
using Account.Features.Users.Domain;
using Account.Integrations.OAuth;
using Account.Integrations.Stripe;
using Bogus;
using JetBrains.Annotations;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using SharedKernel.Authentication;
using SharedKernel.Authentication.TokenGeneration;
using SharedKernel.ExecutionContext;
using SharedKernel.Integrations.Email;
using SharedKernel.SinglePageApp;
using SharedKernel.Telemetry;
using SharedKernel.Tests.Telemetry;

namespace Account.Tests;

public abstract class EndpointBaseTest<TContext> : IDisposable where TContext : DbContext
{
    // Tests use the in-memory test server (WebApplicationFactory); no real listener is bound.
    // SinglePageAppConfiguration only consumes this as a URI for CSP construction.
    private const string TestPublicUrl = "https://localhost";
    protected readonly AccessTokenGenerator AccessTokenGenerator;
    protected readonly IEmailClient EmailClient;
    protected readonly Faker Faker = new();
    protected readonly ServiceCollection Services;
    protected readonly TimeProvider TimeProvider;
    private readonly WebApplicationFactory<Program> _webApplicationFactory;
    protected TelemetryEventsCollectorSpy TelemetryEventsCollectorSpy;

    protected EndpointBaseTest()
    {
        Environment.SetEnvironmentVariable(SinglePageAppConfiguration.PublicUrlKey, TestPublicUrl);
        Environment.SetEnvironmentVariable(SinglePageAppConfiguration.CdnUrlKey, $"{TestPublicUrl}/account");
        Environment.SetEnvironmentVariable(
            "APPLICATIONINSIGHTS_CONNECTION_STRING",
            "InstrumentationKey=00000000-0000-0000-0000-000000000000;IngestionEndpoint=https://localhost;LiveEndpoint=https://localhost"
        );
        Environment.SetEnvironmentVariable("Stripe__AllowMockProvider", "true");
        Environment.SetEnvironmentVariable("Stripe__PublishableKey", "pk_test_mock_publishable_key");

        Services = new ServiceCollection();
        TimeProvider = TimeProvider.System;

        Services.AddLogging();
        Services.AddTransient<DatabaseSeeder>();

        // Create connection using shared cache mode so isolated connections can access the same in-memory database
        Connection = new SqliteConnection($"Data Source=TestDb_{Guid.NewGuid():N};Mode=Memory;Cache=Shared");
        Connection.Open();

        // Configure SQLite to behave more like PostgreSQL
        using (var command = Connection.CreateCommand())
        {
            // Enable foreign key constraints (PostgreSQL has this by default)
            command.CommandText = "PRAGMA foreign_keys = ON;";
            command.ExecuteNonQuery();

            // Enable recursive triggers (PostgreSQL supports nested triggers)
            command.CommandText = "PRAGMA recursive_triggers = ON;";
            command.ExecuteNonQuery();

            // Enforce CHECK constraints (PostgreSQL enforces these by default)
            command.CommandText = "PRAGMA ignore_check_constraints = OFF;";
            command.ExecuteNonQuery();

            // Use more strict query parsing
            command.CommandText = "PRAGMA trusted_schema = OFF;";
            command.ExecuteNonQuery();
        }

        Services.AddDbContext<TContext>(options => { options.UseSqlite(Connection).UseSnakeCaseNamingConvention(); });

        Services.AddAccountServices();

        TelemetryEventsCollectorSpy = new TelemetryEventsCollectorSpy(new TelemetryEventsCollector());
        Services.AddScoped<ITelemetryEventsCollector>(_ => TelemetryEventsCollectorSpy);

        EmailClient = Substitute.For<IEmailClient>();
        Services.AddScoped<IEmailClient>(_ => EmailClient);

        var telemetryChannel = Substitute.For<ITelemetryChannel>();
        Services.AddSingleton(new TelemetryClient(new TelemetryConfiguration { TelemetryChannel = telemetryChannel }));

        Services.AddScoped<IExecutionContext, HttpExecutionContext>();

        // Make sure the database is created
        using var serviceScope = Provider!.CreateScope();
        serviceScope.ServiceProvider.GetRequiredService<TContext>().Database.EnsureCreated();
        DatabaseSeeder = serviceScope.ServiceProvider.GetRequiredService<DatabaseSeeder>();

        AccessTokenGenerator = serviceScope.ServiceProvider.GetRequiredService<AccessTokenGenerator>();

        _webApplicationFactory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            {
                builder.ConfigureLogging(logging =>
                    {
                        logging.AddFilter(_ => false); // Suppress all logs during tests
                    }
                );

                builder.ConfigureAppConfiguration((_, configuration) =>
                    {
                        // Account-api hosts both the user-facing and back-office SPAs scoped via RequireHost
                        // on each MapFallback. The TestServer sends requests to "localhost" by default, so
                        // configure Hostnames:App to match for the user-facing SPA shell.
                        configuration.AddInMemoryCollection(new Dictionary<string, string?>
                            {
                                ["Hostnames:App"] = "localhost"
                            }
                        );
                    }
                );

                builder.ConfigureTestServices(services =>
                    {
                        // Replace the default DbContext in the WebApplication to use an in-memory SQLite database
                        services.Remove(services.Single(d => d.ServiceType == typeof(IDbContextOptionsConfiguration<TContext>)));
                        services.AddDbContext<TContext>(options => { options.UseSqlite(Connection).UseSnakeCaseNamingConvention(); });

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
        AnonymousHttpClient.DefaultRequestHeaders.Add("Cookie", $"{OAuthProviderFactory.UseMockProviderCookieName}=true");

        var ownerUserInfo = CreateUserInfo(DatabaseSeeder.Tenant1Owner, DatabaseSeeder.Tenant1OwnerSession.Id);
        var ownerAccessToken = AccessTokenGenerator.Generate(ownerUserInfo);
        AuthenticatedOwnerHttpClient = _webApplicationFactory.CreateClient();
        AuthenticatedOwnerHttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ownerAccessToken);
        AuthenticatedOwnerHttpClient.DefaultRequestHeaders.Add("Cookie", $"{OAuthProviderFactory.UseMockProviderCookieName}=true");

        var memberUserInfo = CreateUserInfo(DatabaseSeeder.Tenant1Member, DatabaseSeeder.Tenant1MemberSession.Id);
        var memberAccessToken = AccessTokenGenerator.Generate(memberUserInfo);
        AuthenticatedMemberHttpClient = _webApplicationFactory.CreateClient();
        AuthenticatedMemberHttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", memberAccessToken);
        AuthenticatedMemberHttpClient.DefaultRequestHeaders.Add("Cookie", $"{OAuthProviderFactory.UseMockProviderCookieName}=true");

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
            return field ??= Services.BuildServiceProvider();
        }
    }

    protected HttpClient AnonymousHttpClient { get; }

    protected HttpClient AuthenticatedOwnerHttpClient { get; }

    protected HttpClient AuthenticatedMemberHttpClient { get; }

    protected MockStripeState StripeState => _webApplicationFactory.Services.GetRequiredService<MockStripeState>();

    protected IServiceProvider WebApplicationServices => _webApplicationFactory.Services;

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

    private static UserInfo CreateUserInfo(User user, SessionId sessionId)
    {
        return new UserInfo
        {
            IsAuthenticated = true,
            Id = user.Id,
            TenantId = user.TenantId,
            SessionId = sessionId,
            Role = user.Role.ToString(),
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Title = user.Title,
            AvatarUrl = user.Avatar.Url,
            Locale = user.Locale
        };
    }
}
