using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using PlatformPlatform.AccountManagement.Features.Addresses.Queries;

namespace PlatformPlatform.AccountManagement.Integrations.Geoapify;

public sealed class CachedGeoapifyClient(GeoapifyClient geoapifyClient, IMemoryCache memoryCache, ILogger<CachedGeoapifyClient> logger) : IGeoapifyClient
{
    private static readonly TimeSpan CacheDuration = TimeSpan.FromHours(1);

    public async Task<GeoapifyResult> SearchAddressesAsync(string query, string? countryCode, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return new GeoapifyResult(new GeoapifySearchResponse([]), ServiceStatus.Available);
        }

        var cacheKey = $"geoapify_search_{query.ToLowerInvariant()}_{countryCode?.ToLowerInvariant() ?? "any"}";

        if (memoryCache.TryGetValue(cacheKey, out GeoapifyResult? cachedResult))
        {
            logger.LogDebug("Cache hit for Geoapify search query '{Query}' with country '{CountryCode}'", query, countryCode);
            return cachedResult;
        }

        logger.LogDebug("Cache miss for Geoapify search query '{Query}' with country '{CountryCode}'", query, countryCode);

        var result = await geoapifyClient.SearchAddressesAsync(query, countryCode, cancellationToken);

        // Only cache successful results to avoid caching temporary failures
        if (result.ServiceStatus == ServiceStatus.Available)
        {
            memoryCache.Set(cacheKey, result, TimeSpan.FromMinutes(15));
        }

        return result;
    }
}
