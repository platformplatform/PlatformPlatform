using Microsoft.OpenApi;
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
                Schemas = new Dictionary<string, IOpenApiSchema>()
            }
        };

        var proxyConfiguration = proxyConfigProvider.GetConfig();

        foreach (var cluster in proxyConfiguration.Clusters.Where(c => c.ClusterId.EndsWith("-api")))
        {
            OpenApiDocument openApiDocument;
            switch (cluster.ClusterId)
            {
                case "account-api":
                    openApiDocument = await FetchOpenApiDocument(cluster, "ACCOUNT_API_URL");
                    break;
                case "back-office-api":
                    openApiDocument = await FetchOpenApiDocument(cluster, "BACK_OFFICE_API_URL");
                    break;
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
        var (openApiDocument, _) = await OpenApiDocument.LoadAsync(stream);
        return openApiDocument ?? throw new InvalidOperationException($"Failed to load OpenAPI document from {clusterOpenApiUrl}");
    }

    private void CombineOpenApiDocuments(OpenApiDocument aggregatedOpenApiDocument, OpenApiDocument openApiDocument)
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
        if (openApiDocument.Components?.Schemas is not null && aggregatedOpenApiDocument.Components?.Schemas is not null)
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

        var serverUrl = openApiDocument.Servers?.FirstOrDefault()?.Url ?? "unknown";
        logger.LogInformation(
            "Successfully fetched and merged OpenAPI document for {ServerUrl}",
            serverUrl
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
