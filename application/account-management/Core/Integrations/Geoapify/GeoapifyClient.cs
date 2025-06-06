using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PlatformPlatform.AccountManagement.Features.Addresses.Queries;

namespace PlatformPlatform.AccountManagement.Integrations.Geoapify;

public sealed class GeoapifyClient(HttpClient httpClient, ILogger<GeoapifyClient> logger) : IGeoapifyClient
{
    private static readonly string? ApiKey = Environment.GetEnvironmentVariable("GEOAPIFY_API_KEY");

    public async Task<GeoapifyResult> SearchAddressesAsync(string query, string? countryCode, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(ApiKey))
        {
            logger.LogWarning("Geoapify API key is not configured. Address auto-completion is disabled");
            return new GeoapifyResult(
                new GeoapifySearchResponse([]),
                ServiceStatus.NotConfigured,
                "The Geoapify service is not configured"
            );
        }

        try
        {
            var requestUri = $"autocomplete?text={Uri.EscapeDataString(query)}&apiKey={ApiKey}&limit=20&format=json";

            logger.LogDebug("Searching addresses with query '{Query}' and country '{CountryCode}'", query, countryCode);

            var response = await httpClient.GetAsync(requestUri, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var jsonResponse = await response.Content.ReadAsStringAsync(cancellationToken);
                var searchResponse = JsonSerializer.Deserialize<GeoapifySearchResponse>(jsonResponse, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
                });

                return new GeoapifyResult(searchResponse, ServiceStatus.Available);
            }

            logger.LogWarning("Geoapify API returned error status: {StatusCode}", response.StatusCode);
            return new GeoapifyResult(
                new GeoapifySearchResponse([]),
                ServiceStatus.NotResponding,
                "The Geoapify service is not responding"
            );
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred while searching addresses with Geoapify");
            return new GeoapifyResult(
                new GeoapifySearchResponse([]),
                ServiceStatus.NotResponding,
                "The Geoapify service is not responding"
            );
        }
    }
}
