using System.Net.Http.Headers;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PlatformPlatform.SharedKernel.SinglePageApp;
using PlatformPlatform.SharedKernel.TelemetryEvents;
using PlatformPlatform.SharedKernel.Tests.TelemetryEvents;

namespace PlatformPlatform.AccountManagement.Tests;

public abstract class EndpointBaseTest<TContext> : BaseTest<TContext> where TContext : DbContext
{
    private readonly WebApplicationFactory<Program> _webApplicationFactory;

    protected EndpointBaseTest()
    {
        Environment.SetEnvironmentVariable(SinglePageAppConfiguration.PublicUrlKey, "https://localhost:9000");
        Environment.SetEnvironmentVariable(SinglePageAppConfiguration.CdnUrlKey, "https://localhost:9101");

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

        AnonymousHttpClient = _webApplicationFactory.CreateClient();

        var accessToken = AuthenticationTokenGenerator.GenerateAccessToken(DatabaseSeeder.User1);
        AuthenticatedHttpClient = _webApplicationFactory.CreateClient();
        AuthenticatedHttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
    }

    protected HttpClient AnonymousHttpClient { get; }

    protected HttpClient AuthenticatedHttpClient { get; }

    protected override void Dispose(bool disposing)
    {
        _webApplicationFactory.Dispose();
        base.Dispose(disposing);
    }
}