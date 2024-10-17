using Microsoft.OpenApi.Writers;

namespace PlatformPlatform.AppGateway.ApiAggregation;

public static class Endpoints
{
    public static WebApplication MapEndpoints(this WebApplication app)
    {
        app.MapGet("/openapi/v1.json", async (ApiAggregationService aggregationService) =>
            {
                var specification = await aggregationService.GetAggregatedSpecificationAsync();
                await using var stringWriter = new StringWriter();
                var jsonWriter = new OpenApiJsonWriter(stringWriter);
                specification.SerializeAsV3(jsonWriter);
                return Results.Content(stringWriter.ToString(), "application/json");
            })
            .CacheOutput(x => x.Expire(TimeSpan.FromMinutes(5)));

        return app;
    }
}
