using Main.Database;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NSubstitute;
using SharedKernel.ExecutionContext;
using SharedKernel.Integrations.Email;
using SharedKernel.SinglePageApp;
using SharedKernel.Telemetry;

namespace Main.Tests;

// Shared host for all Main API endpoint tests in a class. Constructed once via xUnit's IClassFixture,
// the host wires its DbContext, telemetry collector, and email client to a per-test MainTestContext
// stored in an AsyncLocal — so the same host can serve every test in the class while each test still
// has isolated state.
public class MainWebApplicationFactory : WebApplicationFactory<Program>
{
    // Tests use the in-memory test server (WebApplicationFactory); no real listener is bound.
    // SinglePageAppConfiguration only consumes this as a URI for CSP construction.
    private const string TestPublicUrl = "https://localhost";

    private readonly AsyncLocal<MainTestContext?> _currentContext = new();

    public MainWebApplicationFactory()
    {
        Environment.SetEnvironmentVariable(SinglePageAppConfiguration.PublicUrlKey, TestPublicUrl);
        Environment.SetEnvironmentVariable(SinglePageAppConfiguration.CdnUrlKey, $"{TestPublicUrl}/main");
        Environment.SetEnvironmentVariable(
            "APPLICATIONINSIGHTS_CONNECTION_STRING",
            "InstrumentationKey=00000000-0000-0000-0000-000000000000;IngestionEndpoint=https://localhost;LiveEndpoint=https://localhost"
        );
        // ASP.NET uses a cryptographic double-submit antiforgery pattern that encrypts the user's
        // ClaimUid in the token, which is complex to replicate in tests; the middleware honors this
        // env var to bypass validation.
        Environment.SetEnvironmentVariable("BypassAntiforgeryValidation", "true");
    }

    private MainTestContext CurrentContext => _currentContext.Value
                                              ?? throw new InvalidOperationException("MainTestContext is not set. Call BeginTest before resolving services.");

    // Sets the per-test context for the calling logical-call context. The returned scope clears the
    // context on Dispose so the AsyncLocal does not leak past the test instance lifetime.
    public IDisposable BeginTest(MainTestContext context)
    {
        _currentContext.Value = context;
        return new TestScope(this);
    }

    // TestServer.PreserveExecutionContext defaults to false, which means the calling test's
    // ExecutionContext (and therefore the AsyncLocal-stored MainTestContext) does not flow into request
    // handling. Enabling it preserves the flow so per-request DI resolutions see the current test's
    // context.
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

        builder.ConfigureTestServices(services =>
            {
                services.Remove(services.Single(d => d.ServiceType == typeof(IDbContextOptionsConfiguration<MainDbContext>)));
                services.AddDbContext<MainDbContext>((_, options) => options.UseSqlite(CurrentContext.Connection).UseSnakeCaseNamingConvention());

                services.AddScoped<ITelemetryEventsCollector>(_ => CurrentContext.TelemetryCollector);

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

    private sealed class TestScope(MainWebApplicationFactory factory) : IDisposable
    {
        public void Dispose()
        {
            factory.EndTest();
        }
    }
}
