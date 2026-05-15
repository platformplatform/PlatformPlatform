using System.Net.Http.Headers;
using Account.Features.Users.Domain;
using Account.Integrations.OAuth;
using Account.Integrations.Stripe;
using Bogus;
using JetBrains.Annotations;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using SharedKernel.Authentication;
using SharedKernel.Authentication.TokenGeneration;
using SharedKernel.ExecutionContext;
using SharedKernel.Integrations.Email;
using SharedKernel.Telemetry;
using SharedKernel.Tests.Telemetry;

namespace Account.Tests;

// Base class for Account API endpoint tests. Each derived class declares
// IClassFixture<AccountWebApplicationFactory> (or a subclass) to share a single host across its
// tests; per-test isolation is preserved by the AccountTestContext routed through the fixture's
// AsyncLocal slot.
public abstract class EndpointBaseTest<TContext> : IDisposable where TContext : DbContext
{
    protected readonly AccessTokenGenerator AccessTokenGenerator;
    protected readonly IEmailClient EmailClient;
    protected readonly Faker Faker = new();
    protected readonly ServiceCollection Services;
    protected readonly TimeProvider TimeProvider;
    private readonly AccountWebApplicationFactory _factory;
    private readonly IDisposable _testScope;

    protected EndpointBaseTest(AccountWebApplicationFactory factory)
    {
        _factory = factory;
        TimeProvider = TimeProvider.System;

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

        TelemetryEventsCollectorSpy = new TelemetryEventsCollectorSpy(new TelemetryEventsCollector());
        EmailClient = Substitute.For<IEmailClient>();
        StripeState = new MockStripeState();

        // BeginTest must run before any service resolution on the shared host so the host's
        // hosted services (PlatformCurrencyStartupResolver) and per-request DI lookups see the
        // per-test state.
        _testScope = factory.BeginTest(new AccountTestContext
            {
                Connection = Connection,
                TelemetryCollector = TelemetryEventsCollectorSpy,
                EmailClient = EmailClient,
                StripeState = StripeState
            }
        );

        // The local Services collection is unit-test scaffolding (not part of the WAF). Tests can
        // resolve handlers and repositories directly via Provider without going through HTTP.
        Services = new ServiceCollection();
        Services.AddLogging();
        Services.AddTransient<DatabaseSeeder>();
        Services.AddDbContext<TContext>(options => options.UseSqlite(Connection).UseSnakeCaseNamingConvention());
        Services.AddAccountServices();
        Services.AddScoped<ITelemetryEventsCollector>(_ => TelemetryEventsCollectorSpy);
        Services.AddScoped<IEmailClient>(_ => EmailClient);
        Services.AddSingleton(new TelemetryClient(new TelemetryConfiguration { TelemetryChannel = Substitute.For<ITelemetryChannel>() }));
        Services.AddScoped<IExecutionContext, HttpExecutionContext>();

        // Make sure the database is created
        using var serviceScope = Provider!.CreateScope();
        serviceScope.ServiceProvider.GetRequiredService<TContext>().Database.EnsureCreated();
        DatabaseSeeder = serviceScope.ServiceProvider.GetRequiredService<DatabaseSeeder>();

        AccessTokenGenerator = serviceScope.ServiceProvider.GetRequiredService<AccessTokenGenerator>();

        AnonymousHttpClient = factory.CreateClient();
        AnonymousHttpClient.DefaultRequestHeaders.Add("Cookie", $"{OAuthProviderFactory.UseMockProviderCookieName}=true");

        var ownerUserInfo = CreateUserInfo(DatabaseSeeder.Tenant1Owner, DatabaseSeeder.Tenant1OwnerSession.Id);
        var ownerAccessToken = AccessTokenGenerator.Generate(ownerUserInfo);
        AuthenticatedOwnerHttpClient = factory.CreateClient();
        AuthenticatedOwnerHttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ownerAccessToken);
        AuthenticatedOwnerHttpClient.DefaultRequestHeaders.Add("Cookie", $"{OAuthProviderFactory.UseMockProviderCookieName}=true");

        var memberUserInfo = CreateUserInfo(DatabaseSeeder.Tenant1Member, DatabaseSeeder.Tenant1MemberSession.Id);
        var memberAccessToken = AccessTokenGenerator.Generate(memberUserInfo);
        AuthenticatedMemberHttpClient = factory.CreateClient();
        AuthenticatedMemberHttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", memberAccessToken);
        AuthenticatedMemberHttpClient.DefaultRequestHeaders.Add("Cookie", $"{OAuthProviderFactory.UseMockProviderCookieName}=true");
    }

    protected SqliteConnection Connection { get; }

    protected DatabaseSeeder DatabaseSeeder { get; }

    protected TelemetryEventsCollectorSpy TelemetryEventsCollectorSpy { get; }

    protected MockStripeState StripeState { get; }

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

    protected IServiceProvider WebApplicationServices => _factory.Services;

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    // SonarLint complains if the virtual keyword is missing, as it is required to correctly implement the dispose pattern.
    [UsedImplicitly]
    protected virtual void Dispose(bool disposing)
    {
        if (!disposing) return;
        Provider.Dispose();
        Connection.Close();
        _testScope.Dispose();
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
