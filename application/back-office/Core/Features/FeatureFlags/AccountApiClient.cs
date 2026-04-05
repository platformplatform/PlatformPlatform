using System.Net.Http.Json;
using SharedKernel.Domain;

namespace BackOffice.Features.FeatureFlags;

public sealed class AccountApiClient(HttpClient accountApiHttpClient)
{
    public async Task<HttpResponseMessage> GetFeatureFlagsAsync(CancellationToken cancellationToken)
    {
        return await accountApiHttpClient.GetAsync("/internal-api/account/feature-flags", cancellationToken);
    }

    public async Task<HttpResponseMessage> GetFeatureFlagTenantsAsync(FeatureFlagKey featureFlagKey, CancellationToken cancellationToken)
    {
        return await accountApiHttpClient.GetAsync($"/internal-api/account/feature-flags/{featureFlagKey}/tenants", cancellationToken);
    }

    public async Task<HttpResponseMessage> ActivateFeatureFlagAsync(FeatureFlagKey featureFlagKey, CancellationToken cancellationToken)
    {
        return await accountApiHttpClient.PutAsync($"/internal-api/account/feature-flags/{featureFlagKey}/activate", null, cancellationToken);
    }

    public async Task<HttpResponseMessage> DeactivateFeatureFlagAsync(FeatureFlagKey featureFlagKey, CancellationToken cancellationToken)
    {
        return await accountApiHttpClient.PutAsync($"/internal-api/account/feature-flags/{featureFlagKey}/deactivate", null, cancellationToken);
    }

    public async Task<HttpResponseMessage> SetTenantOverrideAsync(FeatureFlagKey featureFlagKey, long tenantId, bool enabled, CancellationToken cancellationToken)
    {
        return await accountApiHttpClient.PutAsJsonAsync(
            $"/internal-api/account/feature-flags/{featureFlagKey}/tenant-override", new { TenantId = tenantId, Enabled = enabled }, cancellationToken
        );
    }

    public async Task<HttpResponseMessage> SetRolloutPercentageAsync(FeatureFlagKey featureFlagKey, int rolloutPercentage, CancellationToken cancellationToken)
    {
        return await accountApiHttpClient.PutAsJsonAsync(
            $"/internal-api/account/feature-flags/{featureFlagKey}/rollout-percentage", new { RolloutPercentage = rolloutPercentage }, cancellationToken
        );
    }

    public async Task<HttpResponseMessage> RemoveTenantOverrideAsync(FeatureFlagKey featureFlagKey, long tenantId, CancellationToken cancellationToken)
    {
        return await accountApiHttpClient.DeleteAsync($"/internal-api/account/feature-flags/{featureFlagKey}/tenant-override?tenantId={tenantId}", cancellationToken);
    }

    public async Task<HttpResponseMessage> GetFeatureFlagUsersAsync(FeatureFlagKey featureFlagKey, string? search, CancellationToken cancellationToken)
    {
        var url = $"/internal-api/account/feature-flags/{featureFlagKey}/users";
        if (!string.IsNullOrWhiteSpace(search)) url += $"?search={Uri.EscapeDataString(search)}";
        return await accountApiHttpClient.GetAsync(url, cancellationToken);
    }

    public async Task<HttpResponseMessage> SetUserOverrideAsync(FeatureFlagKey featureFlagKey, string userId, long tenantId, bool enabled, CancellationToken cancellationToken)
    {
        return await accountApiHttpClient.PutAsJsonAsync(
            $"/internal-api/account/feature-flags/{featureFlagKey}/user-override", new { UserId = userId, TenantId = tenantId, Enabled = enabled }, cancellationToken
        );
    }

    public async Task<HttpResponseMessage> RemoveUserOverrideAsync(FeatureFlagKey featureFlagKey, string userId, long tenantId, CancellationToken cancellationToken)
    {
        return await accountApiHttpClient.DeleteAsync($"/internal-api/account/feature-flags/{featureFlagKey}/user-override?userId={userId}&tenantId={tenantId}", cancellationToken);
    }
}
