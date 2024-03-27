using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Routing;

namespace PlatformPlatform.SharedKernel.ApiCore.Endpoints;

[UsedImplicitly]
public class HealthEndpoints : IEndpoints
{
    public void MapEndpoints(IEndpointRouteBuilder routes)
    {
        // All health checks must pass for app to be considered ready to accept traffic after starting
        routes.MapHealthChecks("/health");

        // Only health checks tagged with the "live" tag must pass for app to be considered alive
        routes.MapHealthChecks("/alive", new HealthCheckOptions { Predicate = r => r.Tags.Contains("live") });
    }
}