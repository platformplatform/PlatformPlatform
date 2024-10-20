using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Readers;
using Microsoft.OpenApi.Writers;
using Yarp.ReverseProxy.Configuration;

namespace PlatformPlatform.AppGateway.ApiAggregation;

public class ApiAggregationService(
    ILogger<ApiAggregationService> logger,
    IProxyConfigProvider proxyConfigProvider,
    IHttpClientFactory httpClientFactory
)
{
    public async Task<string> GetAggregatedOpenApiJson()
    {
        var openApiDocument = await GetAggregatedOpenApiDocumentAsync();
        var stringWriter = new StringWriter();
        var jsonWriter = new OpenApiJsonWriter(stringWriter);
        openApiDocument.SerializeAsV3(jsonWriter);
        return stringWriter.ToString();
    }

    private async Task<OpenApiDocument> GetAggregatedOpenApiDocumentAsync()
    {
        var aggregatedOpenApiDocument = new OpenApiDocument
        {
            Info = new OpenApiInfo { Title = "PlatformPlatform API", Version = "v1" },
            Paths = new OpenApiPaths(),
            Components = new OpenApiComponents
            {
                Schemas = new Dictionary<string, OpenApiSchema>()
            }
        };

        var proxyConfiguration = proxyConfigProvider.GetConfig();

        foreach (var cluster in proxyConfiguration.Clusters.Where(c => c.ClusterId.EndsWith("-api")))
        {
            OpenApiDocument openApiDocument;
            switch (cluster.ClusterId)
            {
                case "account-management-api":
                    openApiDocument = await FetchOpenApiDocument(cluster, "ACCOUNT_MANAGEMENT_API_URL");
                    break;
                case "back-office-api":
                    openApiDocument = await FetchOpenApiDocument(cluster, "BACK_OFFICE_API_URL");
                    break;
                // Add all clusters that should be part of the public aggregated contract here
                default:
                    continue;
            }

            CombineOpenApiDocuments(aggregatedOpenApiDocument, openApiDocument);
        }

        FilterInternalEndpoints(aggregatedOpenApiDocument);

        return aggregatedOpenApiDocument;
    }

    private async Task<OpenApiDocument> FetchOpenApiDocument(ClusterConfig cluster, string environmentVariable)
    {
        var clusterBasePath = Environment.GetEnvironmentVariable(environmentVariable)
                              ?? cluster.Destinations!.Single().Value.Address;

        var clusterOpenApiUrl = $"{clusterBasePath}/openapi/v1.json";
        logger.LogInformation("Fetching OpenAPI document for cluster {ClusterId} from {Url}", cluster.ClusterId, clusterOpenApiUrl);

        using var httpClient = httpClientFactory.CreateClient();

        var response = await httpClient.GetAsync(clusterOpenApiUrl);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync();
        var reader = new OpenApiStreamReader();
        var openApiDocument = reader.Read(stream, out _);
        return openApiDocument;
    }

    private void CombineOpenApiDocuments(OpenApiDocument aggregatedOpenApiDocument, OpenApiDocument openApiDocument)
    {
        if (openApiDocument.Paths is null) return;

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
                    logger.LogWarning("Duplicate schema found for {SchemaKey}", schema.Key);
                }
                else
                {
                    aggregatedOpenApiDocument.Components.Schemas.Add(schema.Key, schema.Value);
                }
            }
        }

        logger.LogInformation(
            "Successfully fetched and merged OpenAPI document for {ServerUrl}",
            openApiDocument.Servers.Single().Url
        );
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
