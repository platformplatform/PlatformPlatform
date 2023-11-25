using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace PlatformPlatform.SharedKernel.ApiCore.Endpoints;

public static class TestEndpoints
{
    public static void MapTestEndpoints(this IEndpointRouteBuilder routes)
    {
        if (!bool.TryParse(Environment.GetEnvironmentVariable("TestEndpointsEnabled"), out _)) return;

        // Add dummy endpoints to simulate exception throwing for testing
        routes.MapGet("/api/throwException", _ => throw new InvalidOperationException("Simulate an exception."));
        routes.MapGet("/api/throwTimeoutException", _ => throw new TimeoutException("Simulating a timeout exception."));
    }
}