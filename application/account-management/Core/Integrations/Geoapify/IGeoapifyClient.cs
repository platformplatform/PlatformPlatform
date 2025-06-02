namespace PlatformPlatform.AccountManagement.Integrations.Geoapify;

public interface IGeoapifyClient
{
    Task<GeoapifySearchResponse?> SearchAddressesAsync(string query, string? countryCode, CancellationToken cancellationToken);
}
