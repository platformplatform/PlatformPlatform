using Microsoft.OpenApi.Writers;

namespace PlatformPlatform.AppGateway.ApiAggregation;

public static class Endpoints
{
    public static WebApplication ApiAggregationEndpoints(this WebApplication app)
    {
        app.MapGet("/swagger", context =>
            {
                context.Response.Redirect("/openapi/v1");
                return Task.CompletedTask;
            }
        );

        app.MapGet("/openapi", context =>
            {
                context.Response.Redirect("/openapi/v1");
                return Task.CompletedTask;
            }
        );

        app.MapGet("/openapi/v1.json", async (ApiAggregationService openApiAggregationService) =>
                {
                    var openApiDocument = await openApiAggregationService.GetAggregatedSpecificationAsync();
                    await using var stringWriter = new StringWriter();
                    var jsonWriter = new OpenApiJsonWriter(stringWriter);
                    openApiDocument.SerializeAsV3(jsonWriter);
                    return Results.Content(stringWriter.ToString(), "application/json");
                }
            )
            .CacheOutput(c => c.Expire(TimeSpan.FromMinutes(5)));

        return app;
    }
}
