namespace PlatformPlatform.AppGateway.ApiAggregation;

public static class Endpoints
{
    extension(WebApplication app)
    {
        public WebApplication ApiAggregationEndpoints()
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
}
