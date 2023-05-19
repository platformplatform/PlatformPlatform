namespace PlatformPlatform.AccountManagement.WebApi.Endpoints;

public static class TestEndpoints
{
    public static void MapTestEndpoints(this IEndpointRouteBuilder routes)
    {
        if (!bool.TryParse(Environment.GetEnvironmentVariable("TestEndpointsEnabled"), out _)) return;

        // Add a dummy endpoint that throws an exception for testing purposes.
        routes.MapGet("/throwException", _ => throw new Exception("Dummy endpoint for testing."));
    }
}