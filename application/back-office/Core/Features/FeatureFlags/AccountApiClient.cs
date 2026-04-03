using System.Net.Http.Json;

namespace BackOffice.Features.FeatureFlags;

public sealed class AccountApiClient(HttpClient accountApiHttpClient)
{
    public async Task<HttpResponseMessage> GetFeatureFlagsAsync(CancellationToken cancellationToken)
    {
        return await accountApiHttpClient.GetAsync("/internal-api/account/feature-flags", cancellationToken);
    }

    public async Task<HttpResponseMessage> GetFlagTenantsAsync(string flagKey, CancellationToken cancellationToken)
    {
        return await accountApiHttpClient.GetAsync($"/internal-api/account/feature-flags/{flagKey}/tenants", cancellationToken);
    }

    public async Task<HttpResponseMessage> ActivateFlagAsync(string flagKey, CancellationToken cancellationToken)
    {
        return await accountApiHttpClient.PutAsync($"/internal-api/account/feature-flags/{flagKey}/activate", null, cancellationToken);
    }

    public async Task<HttpResponseMessage> DeactivateFlagAsync(string flagKey, CancellationToken cancellationToken)
    {
        return await accountApiHttpClient.PutAsync($"/internal-api/account/feature-flags/{flagKey}/deactivate", null, cancellationToken);
    }

    public async Task<HttpResponseMessage> SetTenantOverrideAsync(string flagKey, long tenantId, bool enabled, CancellationToken cancellationToken)
    {
        return await accountApiHttpClient.PutAsJsonAsync(
            $"/internal-api/account/feature-flags/{flagKey}/tenant-override", new { TenantId = tenantId, Enabled = enabled }, cancellationToken
        );
    }

    public async Task<HttpResponseMessage> SetRolloutPercentageAsync(string flagKey, int rolloutPercentage, CancellationToken cancellationToken)
    {
        return await accountApiHttpClient.PutAsJsonAsync(
            $"/internal-api/account/feature-flags/{flagKey}/rollout-percentage", new { RolloutPercentage = rolloutPercentage }, cancellationToken
        );
    }
}
