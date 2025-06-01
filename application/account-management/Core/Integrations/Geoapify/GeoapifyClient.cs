using System.Text.Json;

namespace PlatformPlatform.AccountManagement.Integrations.Geoapify;

public sealed class GeoapifyClient(HttpClient httpClient, ILogger<GeoapifyClient> logger) : IGeoapifyClient
{
    private static readonly string? ApiKey = Environment.GetEnvironmentVariable("GEOAPIFY_API_KEY");

    public async Task<GeoapifySearchResponse?> SearchAddressesAsync(string query, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(ApiKey))
        {
            logger.LogWarning("Geoapify API key is not configured. Address auto-completion is disabled");
            return new GeoapifySearchResponse([]);
        }

        try
        {
            var requestUri = $"autocomplete?text={Uri.EscapeDataString(query)}&apiKey={ApiKey}&limit=20&format=json";

            var response = await httpClient.GetAsync(requestUri, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogError("Geoapify API request failed with status code {StatusCode} for query '{Query}'", response.StatusCode, query);
                return new GeoapifySearchResponse([]);
            }

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            var searchResponse = JsonSerializer.Deserialize<GeoapifySearchResponse>(content, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
                }
            );

            logger.LogDebug("Geoapify API returned {ResultCount} results for query '{Query}'", searchResponse?.Results?.Length ?? 0, query);

            return searchResponse ?? new GeoapifySearchResponse([]);
        }
        catch (TaskCanceledException ex)
        {
            logger.LogError(ex, "Timeout when calling Geoapify API for query '{Query}'", query);
            return new GeoapifySearchResponse([]);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error calling Geoapify API for query '{Query}'", query);
            return new GeoapifySearchResponse([]);
        }
    }
}
