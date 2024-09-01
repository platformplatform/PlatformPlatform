using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PlatformPlatform.SharedKernel.SinglePageApp;
using PlatformPlatform.SharedKernel.TelemetryEvents;
using PlatformPlatform.SharedKernel.Tests.TelemetryEvents;

namespace PlatformPlatform.BackOffice.Tests.Api;

public abstract class BaseApiTests<TContext> : BaseTest<TContext> where TContext : DbContext
{
    private readonly WebApplicationFactory<Program> _webApplicationFactory;

    protected BaseApiTests()
    {
        Environment.SetEnvironmentVariable(SinglePageAppConfiguration.PublicUrlKey, "https://localhost:9000");
        Environment.SetEnvironmentVariable(SinglePageAppConfiguration.CdnUrlKey, "https://localhost:9201");

        _webApplicationFactory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                    {
                        // Replace the default DbContext in the WebApplication to use an in-memory SQLite database
                        services.Remove(services.Single(d => d.ServiceType == typeof(DbContextOptions<TContext>)));
                        services.AddDbContext<TContext>(options => { options.UseSqlite(Connection); });

                        TelemetryEventsCollectorSpy = new TelemetryEventsCollectorSpy(new TelemetryEventsCollector());
                        services.AddScoped<ITelemetryEventsCollector>(_ => TelemetryEventsCollectorSpy);
                    }
                );
            }
        );

        TestHttpClient = _webApplicationFactory.CreateClient();
    }

    protected HttpClient TestHttpClient { get; }

    protected override void Dispose(bool disposing)
    {
        _webApplicationFactory.Dispose();
        base.Dispose(disposing);
    }
}
