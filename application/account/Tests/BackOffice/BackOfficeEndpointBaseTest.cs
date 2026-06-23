using Account.Integrations.Stripe;
using Bogus;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using SharedKernel.Authentication.BackOfficeIdentity;
using SharedKernel.Authentication.MockEasyAuth;
using SharedKernel.Telemetry;
using SharedKernel.Tests.Telemetry;

namespace Account.Tests.BackOffice;

// Base class for back-office endpoint tests. Each derived class declares
// IClassFixture<BackOfficeWebApplicationFactory> (or a subclass) to share a single host across
// its tests; per-test isolation is preserved by the BackOfficeTestContext routed through the
// fixture's AsyncLocal slot.
public abstract class BackOfficeEndpointBaseTest : IDisposable
{
    protected const string BackOfficeHost = BackOfficeWebApplicationFactory.BackOfficeHost;

    protected readonly Faker Faker = new();
    private readonly BackOfficeWebApplicationFactory _factory;
    private readonly IDisposable _testScope;

    protected BackOfficeEndpointBaseTest(BackOfficeWebApplicationFactory factory)
    {
        _factory = factory;

        Connection = new SqliteConnection($"Data Source=TestDb_{Guid.NewGuid():N};Mode=Memory;Cache=Shared");
        Connection.Open();

        TelemetryEventsCollectorSpy = new TelemetryEventsCollectorSpy(new TelemetryEventsCollector());
        StripeState = new MockStripeState();

        // BeginTest must run before any service resolution so the host's startup hosted services
        // (PlatformCurrencyStartupResolver) see the per-test state.
        _testScope = factory.BeginTest(new BackOfficeTestContext
            {
                Connection = Connection,
                TelemetryCollector = TelemetryEventsCollectorSpy,
                StripeState = StripeState
            }
        );

        // Fill this test's database from the seeded template with a fast binary copy instead of
        // recreating the schema and reseeding per test. The shared seeder's entity references match the
        // rows copied into this connection.
        DatabaseSeeder = SeededDatabaseTemplate.EnsureSeeded();
        SeededDatabaseTemplate.RestoreInto(Connection);
    }

    protected SqliteConnection Connection { get; }

    protected DatabaseSeeder DatabaseSeeder { get; }

    protected TelemetryEventsCollectorSpy TelemetryEventsCollectorSpy { get; }

    protected MockStripeState StripeState { get; }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected HttpClient CreateBackOfficeClient(string? clientPrincipalName = null, string? clientPrincipalId = null, string? clientPrincipalPayload = null)
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                BaseAddress = new Uri($"https://{BackOfficeHost}"),
                AllowAutoRedirect = false
            }
        );
        client.DefaultRequestHeaders.Host = BackOfficeHost;
        if (clientPrincipalName is not null) client.DefaultRequestHeaders.Add(BackOfficeIdentityDefaults.PrincipalNameHeader, clientPrincipalName);
        if (clientPrincipalId is not null) client.DefaultRequestHeaders.Add(BackOfficeIdentityDefaults.PrincipalIdHeader, clientPrincipalId);
        if (clientPrincipalPayload is not null) client.DefaultRequestHeaders.Add(BackOfficeIdentityDefaults.PrincipalPayloadHeader, clientPrincipalPayload);
        return client;
    }

    protected HttpClient CreateBackOfficeClientForIdentity(MockEasyAuthIdentity identity)
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                BaseAddress = new Uri($"https://{BackOfficeHost}"),
                AllowAutoRedirect = false
            }
        );
        client.DefaultRequestHeaders.Host = BackOfficeHost;
        client.DefaultRequestHeaders.Add(BackOfficeIdentityDefaults.PrincipalNameHeader, identity.Name);
        client.DefaultRequestHeaders.Add(BackOfficeIdentityDefaults.PrincipalIdHeader, identity.ObjectId);
        client.DefaultRequestHeaders.Add(BackOfficeIdentityDefaults.PrincipalPayloadHeader, MockEasyAuthCookie.EncodePayload(identity));
        return client;
    }

    protected HttpClient CreateClientForHost(string host)
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                BaseAddress = new Uri($"https://{host}"),
                AllowAutoRedirect = false
            }
        );
        client.DefaultRequestHeaders.Host = host;
        return client;
    }

    [UsedImplicitly]
    protected virtual void Dispose(bool disposing)
    {
        if (!disposing) return;
        Connection.Close();
        _testScope.Dispose();
    }
}
