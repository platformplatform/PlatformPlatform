using Account.Features.FeatureFlags.Domain;
using SharedKernel.Domain;

namespace Account.Features.FeatureFlags.Shared;

public sealed class FeatureFlagEvaluator(IFeatureFlagRepository featureFlagRepository)
{
    public async Task<IReadOnlyList<string>> EvaluateAsync(TenantId tenantId, UserId userId, int tenantRolloutBucket, int userRolloutBucket, CancellationToken cancellationToken)
    {
        var allFeatureFlags = await featureFlagRepository.GetAllRelevantFeatureFlagsAsync(tenantId, userId, cancellationToken);
        var enabledFeatureFlags = new List<string>();

        foreach (var featureFlagDefinition in SharedKernel.Domain.FeatureFlags.GetAll())
        {
            if (featureFlagDefinition.Scope == FeatureFlagScope.System) continue;

            var baseFeatureFlag = allFeatureFlags.FirstOrDefault(f => f.FeatureFlagKey == featureFlagDefinition.Key && f.TenantId is null && f.UserId is null);
            if (baseFeatureFlag is null) continue;

            if (!IsActive(baseFeatureFlag)) continue;

            var isEnabled = featureFlagDefinition.Scope switch
            {
                FeatureFlagScope.Tenant => EvaluateTenantScope(featureFlagDefinition, baseFeatureFlag, allFeatureFlags, tenantId, tenantRolloutBucket),
                FeatureFlagScope.User => EvaluateUserScope(featureFlagDefinition, baseFeatureFlag, allFeatureFlags, tenantId, userId, userRolloutBucket),
                _ => false
            };

            if (!isEnabled) continue;

            enabledFeatureFlags.Add(featureFlagDefinition.Key);
        }

        return enabledFeatureFlags;
    }

    private static bool EvaluateTenantScope(FeatureFlagDefinition featureFlagDefinition, FeatureFlag baseFeatureFlag, FeatureFlag[] allFeatureFlags, TenantId tenantId, int tenantRolloutBucket)
    {
        var tenantFeatureFlag = allFeatureFlags.FirstOrDefault(f => f.FeatureFlagKey == featureFlagDefinition.Key && f.TenantId == tenantId && f.UserId is null);
        if (tenantFeatureFlag is not null)
        {
            return IsActive(tenantFeatureFlag);
        }

        if (featureFlagDefinition.IsAbTestEligible && baseFeatureFlag.RolloutBucketStart is not null && baseFeatureFlag.RolloutBucketEnd is not null)
        {
            return RolloutBucketHasher.IsInRolloutBucketRange(tenantRolloutBucket, baseFeatureFlag.RolloutBucketStart.Value, baseFeatureFlag.RolloutBucketEnd.Value);
        }

        return false;
    }

    private static bool EvaluateUserScope(FeatureFlagDefinition featureFlagDefinition, FeatureFlag baseFeatureFlag, FeatureFlag[] allFeatureFlags, TenantId tenantId, UserId userId, int userRolloutBucket)
    {
        var userFeatureFlag = allFeatureFlags.FirstOrDefault(f => f.FeatureFlagKey == featureFlagDefinition.Key && f.TenantId == tenantId && f.UserId == userId);
        if (userFeatureFlag is not null)
        {
            return IsActive(userFeatureFlag);
        }

        if (featureFlagDefinition.IsAbTestEligible && baseFeatureFlag.RolloutBucketStart is not null && baseFeatureFlag.RolloutBucketEnd is not null)
        {
            return RolloutBucketHasher.IsInRolloutBucketRange(userRolloutBucket, baseFeatureFlag.RolloutBucketStart.Value, baseFeatureFlag.RolloutBucketEnd.Value);
        }

        return false;
    }

    private static bool IsActive(FeatureFlag featureFlag)
    {
        return featureFlag.EnabledAt is not null && (featureFlag.DisabledAt is null || featureFlag.EnabledAt > featureFlag.DisabledAt);
    }
}
