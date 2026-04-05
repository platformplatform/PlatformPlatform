using Account.Features.FeatureFlags.Domain;
using SharedKernel.Domain;

namespace Account.Features.FeatureFlags.Shared;

public sealed class TenantFeatureFlagToggler(IFeatureFlagRepository featureFlagRepository)
{
    public async Task EnableAsync(FeatureFlagKey featureFlagKey, TenantId tenantId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var tenantFeatureFlag = await featureFlagRepository.GetByKeyAndTenantAsync(featureFlagKey, tenantId, cancellationToken);
        if (tenantFeatureFlag is null)
        {
            tenantFeatureFlag = FeatureFlag.CreateTenantOverride(featureFlagKey, tenantId);
            tenantFeatureFlag.Activate(now);
            await featureFlagRepository.AddAsync(tenantFeatureFlag, cancellationToken);
        }
        else
        {
            tenantFeatureFlag.Activate(now);
            featureFlagRepository.Update(tenantFeatureFlag);
        }
    }

    public async Task DisableAsync(FeatureFlagKey featureFlagKey, TenantId tenantId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var tenantFeatureFlag = await featureFlagRepository.GetByKeyAndTenantAsync(featureFlagKey, tenantId, cancellationToken);
        if (tenantFeatureFlag is null)
        {
            tenantFeatureFlag = FeatureFlag.CreateTenantOverride(featureFlagKey, tenantId);
            await featureFlagRepository.AddAsync(tenantFeatureFlag, cancellationToken);
        }
        else
        {
            tenantFeatureFlag.Deactivate(now);
            featureFlagRepository.Update(tenantFeatureFlag);
        }
    }
}
