using Microsoft.Extensions.Caching.Memory;

namespace PlatformPlatform.AccountManagement.Integrations.Geoapify;

public sealed class CachedGeoapifyClient(GeoapifyClient geoapifyClient, IMemoryCache memoryCache, ILogger<CachedGeoapifyClient> logger) : IGeoapifyClient
{
    private static readonly TimeSpan CacheDuration = TimeSpan.FromHours(1);

    public async Task<GeoapifySearchResponse?> SearchAddressesAsync(string query, string? countryCode, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return new GeoapifySearchResponse([]);
        }

        var cacheKey = $"geoapify_search_{query.ToLowerInvariant()}_{countryCode?.ToLowerInvariant() ?? "any"}";

        if (memoryCache.TryGetValue(cacheKey, out GeoapifySearchResponse? cachedResult))
        {
            logger.LogDebug("Cache hit for Geoapify search query '{Query}' with country '{CountryCode}'", query, countryCode);
            return cachedResult;
        }

        logger.LogDebug("Cache miss for Geoapify search query '{Query}' with country '{CountryCode}', calling underlying client", query, countryCode);
        var result = await geoapifyClient.SearchAddressesAsync(query, countryCode, cancellationToken);

        if (result != null)
        {
            memoryCache.Set(cacheKey, result, CacheDuration);
            logger.LogDebug("Cached Geoapify search result for query '{Query}' with country '{CountryCode}' for {Duration}", query, countryCode, CacheDuration);
        }

        return result;
    }
}
