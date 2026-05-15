using Account.Database;
using Account.Integrations.Stripe;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using NSubstitute;
using SharedKernel.ExecutionContext;
using SharedKernel.Integrations.Email;
using SharedKernel.SinglePageApp;
using SharedKernel.Telemetry;

namespace Account.Tests;

// Shared host for all Account API endpoint tests in a class. Constructed once via xUnit's
// IClassFixture, the host wires its DbContext, telemetry collector, email client, and Stripe
// state to a per-test AccountTestContext stored in an AsyncLocal — so the same host can serve
// every test in the class while each test still has isolated state.
public class AccountWebApplicationFactory : WebApplicationFactory<Program>
{
    // Tests use the in-memory test server (WebApplicationFactory); no real listener is bound.
    // SinglePageAppConfiguration only consumes this as a URI for CSP construction.
    private const string TestPublicUrl = "https://localhost";

    private readonly AsyncLocal<AccountTestContext?> _currentContext = new();

    public AccountWebApplicationFactory()
    {
        Environment.SetEnvironmentVariable(SinglePageAppConfiguration.PublicUrlKey, TestPublicUrl);
        Environment.SetEnvironmentVariable(SinglePageAppConfiguration.CdnUrlKey, $"{TestPublicUrl}/account");
        Environment.SetEnvironmentVariable(
            "APPLICATIONINSIGHTS_CONNECTION_STRING",
            "InstrumentationKey=00000000-0000-0000-0000-000000000000;IngestionEndpoint=https://localhost;LiveEndpoint=https://localhost"
        );
        Environment.SetEnvironmentVariable("Stripe__AllowMockProvider", "true");
        Environment.SetEnvironmentVariable("Stripe__PublishableKey", "pk_test_mock_publishable_key");
        // ASP.NET uses a cryptographic double-submit antiforgery pattern that encrypts the user's
        // ClaimUid in the token, which is complex to replicate in tests; the middleware honors this
        // env var to bypass validation.
        Environment.SetEnvironmentVariable("BypassAntiforgeryValidation", "true");
    }

    private AccountTestContext CurrentContext => _currentContext.Value
                                                 ?? throw new InvalidOperationException("AccountTestContext is not set. Call BeginTest before resolving services.");

    // Sets the per-test context for the calling logical-call context. The returned scope clears the
    // context on Dispose so the AsyncLocal does not leak past the test instance lifetime.
    public IDisposable BeginTest(AccountTestContext context)
    {
        _currentContext.Value = context;
        return new TestScope(this);
    }

    // TestServer.PreserveExecutionContext defaults to false, which means the calling test's
    // ExecutionContext (and therefore the AsyncLocal-stored AccountTestContext) does not flow into
    // request handling. Enabling it preserves the flow so per-request DI resolutions see the
    // current test's context.
    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);
        if (host.Services.GetRequiredService<IServer>() is TestServer testServer)
        {
            testServer.PreserveExecutionContext = true;
        }

        return host;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureLogging(logging => logging.AddFilter(_ => false));

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
                services.Remove(services.Single(d => d.ServiceType == typeof(IDbContextOptionsConfiguration<AccountDbContext>)));
                services.AddDbContext<AccountDbContext>((_, options) => options.UseSqlite(CurrentContext.Connection).UseSnakeCaseNamingConvention());

                services.AddScoped<ITelemetryEventsCollector>(_ => CurrentContext.TelemetryCollector);

                // Replace the production singleton with a transient that resolves the per-test
                // instance from the current AccountTestContext. MockStripeClient is keyed-scoped
                // and captures MockStripeState per scope, so this delegates state mutations to the
                // active test.
                services.RemoveAll(typeof(MockStripeState));
                services.AddTransient<MockStripeState>(_ => CurrentContext.StripeState);

                services.Remove(services.Single(d => d.ServiceType == typeof(IEmailClient)));
                services.AddTransient<IEmailClient>(_ => CurrentContext.EmailClient);

                services.AddSingleton(new TelemetryClient(new TelemetryConfiguration { TelemetryChannel = Substitute.For<ITelemetryChannel>() }));
                services.AddScoped<IExecutionContext, HttpExecutionContext>();

                ConfigureAdditionalTestServices(services);
            }
        );
    }

    protected virtual void ConfigureAdditionalTestServices(IServiceCollection services)
    {
    }

    private void EndTest()
    {
        _currentContext.Value = null;
    }

    private sealed class TestScope(AccountWebApplicationFactory factory) : IDisposable
    {
        public void Dispose()
        {
            factory.EndTest();
        }
    }
}
