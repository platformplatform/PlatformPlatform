using Account.Features.FeatureFlags.Domain;
using SharedKernel.Domain;

namespace Account.Features.FeatureFlags.Shared;

public sealed class UserFeatureFlagToggler(IFeatureFlagRepository featureFlagRepository)
{
    public async Task EnableAsync(string featureFlagKey, TenantId tenantId, UserId userId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var userFeatureFlag = await featureFlagRepository.GetByKeyAndUserAsync(featureFlagKey, tenantId, userId, cancellationToken);
        if (userFeatureFlag is null)
        {
            userFeatureFlag = FeatureFlag.CreateUserOverride(featureFlagKey, tenantId, userId);
            userFeatureFlag.Activate(now);
            await featureFlagRepository.AddAsync(userFeatureFlag, cancellationToken);
        }
        else
        {
            userFeatureFlag.Activate(now);
            featureFlagRepository.Update(userFeatureFlag);
        }
    }

    public async Task DisableAsync(string featureFlagKey, TenantId tenantId, UserId userId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var userFeatureFlag = await featureFlagRepository.GetByKeyAndUserAsync(featureFlagKey, tenantId, userId, cancellationToken);
        if (userFeatureFlag is null)
        {
            userFeatureFlag = FeatureFlag.CreateUserOverride(featureFlagKey, tenantId, userId);
            await featureFlagRepository.AddAsync(userFeatureFlag, cancellationToken);
        }
        else
        {
            userFeatureFlag.Deactivate(now);
            featureFlagRepository.Update(userFeatureFlag);
        }
    }
}
