using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Readers;
using Yarp.ReverseProxy.Configuration;

namespace PlatformPlatform.AppGateway.ApiAggregation;

public class ApiAggregationService(ILogger<ApiAggregationService> logger, IConfiguration configuration, IHttpClientFactory httpClientFactory)
{
    public async Task<OpenApiDocument> GetAggregatedSpecificationAsync()
    {
        var aggregatedSpecification = new OpenApiDocument
        {
            Info = new OpenApiInfo { Title = "PlatformPlatform API", Version = "v1" },
            Paths = new OpenApiPaths(),
            Components = new OpenApiComponents
            {
                Schemas = new Dictionary<string, OpenApiSchema>()
            }
        };

        var clusters = configuration.GetSection("ReverseProxy:Clusters").Get<Dictionary<string, ClusterConfig>>()!;

        foreach (var cluster in clusters!.Where(c => c.Key.EndsWith("-api")))
        {
            try
            {
                var url = $"{cluster.Value.Destinations!.Single().Value.Address}/swagger/v1/swagger.json";
                var specification = await FetchSpecificationAsync(url);
                CombineOpenApiDocuments(aggregatedSpecification, specification);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to fetch or merge specification for cluster {ClusterKey}", cluster.Key);
            }
        }

        FilterInternalEndpoints(aggregatedSpecification);
        return aggregatedSpecification;
    }

    private async Task<OpenApiDocument> FetchSpecificationAsync(string url)
    {
        using var httpClient = httpClientFactory.CreateClient();
        var response = await httpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync();
        var reader = new OpenApiStreamReader();
        return reader.Read(stream, out _);
    }

    private static void CombineOpenApiDocuments(OpenApiDocument aggregatedOpenApiDocument, OpenApiDocument openApiDocument)
    {
        // Merge paths
        foreach (var path in openApiDocument.Paths)
        {
            if (!aggregatedOpenApiDocument.Paths.ContainsKey(path.Key))
            {
                aggregatedOpenApiDocument.Paths.Add(path.Key, path.Value);
            }
        }

        // Merge schemas
        if (openApiDocument.Components?.Schemas is not null)
        {
            foreach (var schema in openApiDocument.Components.Schemas)
            {
                if (aggregatedOpenApiDocument.Components.Schemas.ContainsKey(schema.Key))
                {
                    Console.Error.WriteLine($"Duplicate schema for {schema.Key}");
                }
                else
                {
                    aggregatedOpenApiDocument.Components.Schemas.Add(schema.Key, schema.Value);
                }
            }
        }
    }

    private static void FilterInternalEndpoints(OpenApiDocument openApiDocument)
    {
        var internalPaths = openApiDocument.Paths
            .Where(p => p.Key.StartsWith("/internal-api/"))
            .Select(p => p.Key)
            .ToArray();

        foreach (var path in internalPaths)
        {
            openApiDocument.Paths.Remove(path);
        }
    }
}
