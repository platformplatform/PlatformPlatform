using Microsoft.OpenApi.Writers;

namespace PlatformPlatform.AppGateway.ApiAggregation;

public static class Endpoints
{
    public static void MapEndpoints(WebApplication app)
    {
        app.MapGet("/api-docs", () =>
            {
                const string htmlContent =
                    """
                    <!DOCTYPE html>
                    <html>
                      <head>
                        <title>PlatformPlatform API Documentation</title>
                        <meta charset="utf-8"/>
                        <meta name="viewport" content="width=device-width, initial-scale=1">
                        <link href="https://fonts.googleapis.com/css?family=Montserrat:300,400,700|Roboto:300,400,700" rel="stylesheet">
                        <style>
                          body {
                            margin: 0;
                            padding: 0;
                          }
                        </style>
                      </head>
                      <body>
                        <redoc spec-url='/api-docs/openapi.json'></redoc>
                        <script src="https://cdn.redoc.ly/redoc/latest/bundles/redoc.standalone.js"> </script>
                      </body>
                    </html>
                    """;
                return Results.Content(htmlContent, "text/html");
            }
        );

        app.MapGet("/api-docs/openapi.json", async (ApiAggregationService aggregationService) =>
                {
                    var specification = await aggregationService.GetAggregatedSpecificationAsync();
                    await using var stringWriter = new StringWriter();
                    var jsonWriter = new OpenApiJsonWriter(stringWriter);
                    specification.SerializeAsV3(jsonWriter);
                    return Results.Content(stringWriter.ToString(), "application/json");
                }
            )
            .CacheOutput(x => x.Expire(TimeSpan.FromMinutes(5)));
    }
}
