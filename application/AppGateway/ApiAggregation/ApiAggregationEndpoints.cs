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

        app.MapGet("/openapi/v1.json", async (ApiAggregationService apiAggregationService)
            => Results.Content(await apiAggregationService.GetAggregatedOpenApiJson(), "application/json")
        ).CacheOutput(c => c.Expire(TimeSpan.FromMinutes(5)));

        return app;
    }
}
