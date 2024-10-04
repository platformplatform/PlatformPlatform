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
                MergeSpecification(aggregatedSpecification, specification);
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

    private void MergeSpecification(OpenApiDocument aggregatedSpecification, OpenApiDocument specification)
    {
        // Merge paths
        foreach (var path in specification.Paths)
        {
            if (!aggregatedSpecification.Paths.ContainsKey(path.Key))
            {
                aggregatedSpecification.Paths.Add(path.Key, path.Value);
            }
        }

        // Merge schemas
        if (specification.Components?.Schemas is not null)
        {
            foreach (var schema in specification.Components.Schemas)
            {
                if (!aggregatedSpecification.Components.Schemas.ContainsKey(schema.Key))
                {
                    aggregatedSpecification.Components.Schemas.Add(schema.Key, schema.Value);
                }
            }
        }

        // Merge other component types (e.g., responses, parameters, examples, etc.)
        // Add similar merging logic for other component types as needed
    }

    private void FilterInternalEndpoints(OpenApiDocument specification)
    {
        var internalPaths = specification.Paths
            .Where(p => p.Key.StartsWith("/internal-api/"))
            .Select(p => p.Key)
            .ToArray();

        foreach (var path in internalPaths)
        {
            specification.Paths.Remove(path);
        }
    }
}
