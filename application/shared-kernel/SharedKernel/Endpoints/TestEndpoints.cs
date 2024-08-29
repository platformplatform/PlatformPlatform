using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace PlatformPlatform.SharedKernel.Endpoints;

public class TestEndpoints : IEndpoints
{
    public void MapEndpoints(IEndpointRouteBuilder routes)
    {
        if (!bool.TryParse(Environment.GetEnvironmentVariable("TestEndpointsEnabled"), out _)) return;

        // These endpoints are only enabled when running tests, and serve to simulate exception throwing for testing
        routes.MapGet("/api/throwException", _ => throw new InvalidOperationException("Simulate an exception.")).AllowAnonymous();
        routes.MapGet("/api/throwTimeoutException", _ => throw new TimeoutException("Simulating a timeout exception.")).AllowAnonymous();
    }
}
