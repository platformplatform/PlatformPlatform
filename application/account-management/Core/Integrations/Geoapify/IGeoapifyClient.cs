using PlatformPlatform.AccountManagement.Features.Addresses.Queries;

namespace PlatformPlatform.AccountManagement.Integrations.Geoapify;

public interface IGeoapifyClient
{
    Task<GeoapifyResult> SearchAddressesAsync(string query, string? countryCode, CancellationToken cancellationToken);
}

public sealed record GeoapifyResult(GeoapifySearchResponse? Response, ServiceStatus ServiceStatus, string? ServiceMessage = null);
