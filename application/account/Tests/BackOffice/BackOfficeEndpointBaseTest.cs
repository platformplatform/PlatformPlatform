using Account.Database;
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
using SharedKernel.Authentication.BackOfficeIdentity;
using SharedKernel.Authentication.MockEasyAuth;
using SharedKernel.ExecutionContext;
using SharedKernel.Integrations.Email;
using SharedKernel.SinglePageApp;
using SharedKernel.Telemetry;
using SharedKernel.Tests.Telemetry;

namespace Account.Tests.BackOffice;

// Base class for back-office endpoint tests. Configures the BackOffice host (so RequireHost matches)
// and provides helpers to build HTTP clients with the right Host header and X-MS-CLIENT-PRINCIPAL-* headers.
public abstract class BackOfficeEndpointBaseTest : IDisposable
{
    protected const string BackOfficeHost = "back-office.test.localhost";

    private const string TestPublicUrl = "https://localhost";

    private static readonly Lock SpaShellLock = new();
    protected readonly Faker Faker = new();
    private readonly WebApplicationFactory<Program> _webApplicationFactory;

    protected BackOfficeEndpointBaseTest()
    {
        Environment.SetEnvironmentVariable(SinglePageAppConfiguration.PublicUrlKey, TestPublicUrl);
        Environment.SetEnvironmentVariable(SinglePageAppConfiguration.CdnUrlKey, $"{TestPublicUrl}/account");
        Environment.SetEnvironmentVariable(
            "APPLICATIONINSIGHTS_CONNECTION_STRING",
            "InstrumentationKey=00000000-0000-0000-0000-000000000000;IngestionEndpoint=https://localhost;LiveEndpoint=https://localhost"
        );
        Environment.SetEnvironmentVariable("Stripe__AllowMockProvider", "true");
        Environment.SetEnvironmentVariable("Stripe__PublishableKey", "pk_test_mock_publishable_key");

        EnsureBackOfficeSpaShell();

        TelemetryEventsCollectorSpy = new TelemetryEventsCollectorSpy(new TelemetryEventsCollector());

        Connection = new SqliteConnection($"Data Source=TestDb_{Guid.NewGuid():N};Mode=Memory;Cache=Shared");
        Connection.Open();

        _webApplicationFactory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            {
                builder.ConfigureLogging(logging => logging.AddFilter(_ => false));

                builder.ConfigureAppConfiguration((_, configuration) =>
                    {
                        var backOfficeSettings = new Dictionary<string, string?>
                        {
                            ["BackOffice:Host"] = BackOfficeHost,
                            // Match the AppHost wiring: mock admin identity carries this group id, so
                            // configuring it here lets BackOfficeAdminAuthorizationHandler and GetMe.IsAdmin
                            // resolve admin status the same way they do in dev.
                            ["BackOffice:AdminsGroupId"] = MockEasyAuthIdentities.MockAdminsGroupId,
                            // The user-facing SPA shell is scoped to Hostnames:App via UseHostScopedSinglePageAppFallback.
                            // Tests that target the user-facing host use app.test.localhost.
                            ["Hostnames:App"] = "app.test.localhost"
                        };

                        configuration.AddInMemoryCollection(backOfficeSettings);
                    }
                );

                builder.ConfigureTestServices(services =>
                    {
                        services.Remove(services.Single(d => d.ServiceType == typeof(IDbContextOptionsConfiguration<AccountDbContext>)));
                        services.AddDbContext<AccountDbContext>(options => options.UseSqlite(Connection).UseSnakeCaseNamingConvention());

                        services.AddScoped<ITelemetryEventsCollector>(_ => TelemetryEventsCollectorSpy);

                        services.Remove(services.Single(d => d.ServiceType == typeof(IEmailClient)));
                        services.AddTransient<IEmailClient>(_ => Substitute.For<IEmailClient>());

                        services.AddSingleton(new TelemetryClient(new TelemetryConfiguration { TelemetryChannel = Substitute.For<ITelemetryChannel>() }));
                        services.AddScoped<IExecutionContext, HttpExecutionContext>();
                    }
                );
            }
        );

        using var scope = _webApplicationFactory.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<AccountDbContext>().Database.EnsureCreated();
        DatabaseSeeder = ActivatorUtilities.CreateInstance<DatabaseSeeder>(scope.ServiceProvider);

        Environment.SetEnvironmentVariable("BypassAntiforgeryValidation", "true");
    }

    protected SqliteConnection Connection { get; }

    protected DatabaseSeeder DatabaseSeeder { get; }

    protected TelemetryEventsCollectorSpy TelemetryEventsCollectorSpy { get; }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    // SinglePageAppConfiguration.GetHtmlTemplate() reads BackOffice/dist/index.html on every SPA-shell
    // request. Locally that file is generated by `rsbuild dev`; in CI the test step runs before any frontend
    // build, so the file is missing and the fallback returns 500. The dist's index.html is just the public
    // template plus rsbuild's bundle <script> injection — we don't need that here, so seed dist/index.html
    // from public/index.html when it is missing or when a previous failed `rsbuild dev` left a broken
    // artifact (no <body id="back-office">). The static Lock serializes parallel test class constructors;
    // File.Copy opens the destination with FileShare.None and concurrent writers hit IOException.
    private static void EnsureBackOfficeSpaShell()
    {
        // Walk up looking for the account folder (the parent of both Tests/BackOffice and BackOffice).
        // Matching just on "BackOffice" would stop at Tests/BackOffice, which is this test fixture's
        // own folder, not the SPA bundle.
        var directory = new DirectoryInfo(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!);
        while (directory is not null && !Directory.Exists(Path.Combine(directory.FullName, "BackOffice", "public")))
        {
            directory = directory.Parent;
        }

        if (directory is null) return;

        var distDirectory = Path.Combine(directory.FullName, "BackOffice", "dist");
        var distIndexPath = Path.Combine(distDirectory, "index.html");
        var publicIndexPath = Path.Combine(directory.FullName, "BackOffice", "public", "index.html");

        lock (SpaShellLock)
        {
            if (File.Exists(distIndexPath) && File.ReadAllText(distIndexPath).Contains("id=\"back-office\"", StringComparison.Ordinal)) return;
            if (!File.Exists(publicIndexPath)) return;

            Directory.CreateDirectory(distDirectory);
            File.Copy(publicIndexPath, distIndexPath, true);
        }
    }

    protected HttpClient CreateBackOfficeClient(string? clientPrincipalName = null, string? clientPrincipalId = null, string? clientPrincipalPayload = null)
    {
        var client = _webApplicationFactory.CreateClient(new WebApplicationFactoryClientOptions
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
        var client = _webApplicationFactory.CreateClient(new WebApplicationFactoryClientOptions
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
        var client = _webApplicationFactory.CreateClient(new WebApplicationFactoryClientOptions
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
        _webApplicationFactory.Dispose();
    }
}
