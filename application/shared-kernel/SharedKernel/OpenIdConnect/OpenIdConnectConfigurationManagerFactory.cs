using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace PlatformPlatform.SharedKernel.OpenIdConnect;

public sealed class OpenIdConnectConfigurationManagerFactory(HttpClient httpClient, ILoggerFactory loggerFactory)
{
    private static readonly TimeSpan AutomaticRefreshInterval = TimeSpan.FromHours(24);
    private static readonly TimeSpan RefreshInterval = TimeSpan.FromMinutes(5);

    private readonly Dictionary<string, ConfigurationManager<OpenIdConnectConfiguration>> _configurationManagers = new();
    private readonly Lock _lock = new();

    public ConfigurationManager<OpenIdConnectConfiguration> GetOrCreate(string domain)
    {
        lock (_lock)
        {
            if (!_configurationManagers.TryGetValue(domain, out var configurationManager))
            {
                var authority = $"https://{domain}";
                configurationManager = new ConfigurationManager<OpenIdConnectConfiguration>(
                    $"{authority}/.well-known/openid-configuration",
                    new OpenIdConnectConfigurationRetriever(),
                    new HttpDocumentRetriever(httpClient, loggerFactory.CreateLogger<HttpDocumentRetriever>())
                )
                {
                    AutomaticRefreshInterval = AutomaticRefreshInterval,
                    RefreshInterval = RefreshInterval
                };
                _configurationManagers[domain] = configurationManager;
            }

            return configurationManager;
        }
    }

    private sealed class HttpDocumentRetriever(HttpClient httpClient, ILogger<HttpDocumentRetriever> logger) : IDocumentRetriever
    {
        public async Task<string> GetDocumentAsync(string address, CancellationToken cancel)
        {
            try
            {
                var response = await httpClient.GetAsync(address, cancel);
                if (!response.IsSuccessStatusCode)
                {
                    logger.LogError("Failed to retrieve document from '{Address}'. Status: '{StatusCode}'", address, response.StatusCode);
                    return string.Empty;
                }

                return await response.Content.ReadAsStringAsync(cancel);
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Error retrieving document from '{Address}'", address);
                return string.Empty;
            }
        }
    }
}
