using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace PlatformPlatform.SharedKernel.OpenIdConnect;

public sealed class OpenIdConnectConfigurationManagerFactory(HttpClient httpClient)
{
    private static readonly TimeSpan AutomaticRefreshInterval = TimeSpan.FromHours(24);
    private static readonly TimeSpan RefreshInterval = TimeSpan.FromMinutes(5);

    private readonly Dictionary<string, ConfigurationManager<OpenIdConnectConfiguration>> _configurationManagers = new();
    private readonly Lock _lock = new();

    public ConfigurationManager<OpenIdConnectConfiguration> GetOrCreate(string discoveryUrl)
    {
        lock (_lock)
        {
            if (!_configurationManagers.TryGetValue(discoveryUrl, out var configurationManager))
            {
                configurationManager = new ConfigurationManager<OpenIdConnectConfiguration>(
                    discoveryUrl,
                    new OpenIdConnectConfigurationRetriever(),
                    new HttpDocumentRetriever(httpClient)
                )
                {
                    AutomaticRefreshInterval = AutomaticRefreshInterval,
                    RefreshInterval = RefreshInterval
                };
                _configurationManagers[discoveryUrl] = configurationManager;
            }

            return configurationManager;
        }
    }

    private sealed class HttpDocumentRetriever(HttpClient httpClient) : IDocumentRetriever
    {
        public async Task<string> GetDocumentAsync(string address, CancellationToken cancel)
        {
            var response = await httpClient.GetAsync(address, cancel);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync(cancel);
        }
    }
}
