using Account.Features.FeatureFlags.Domain;
using SharedKernel.Domain;

namespace Account.Features.FeatureFlags.Shared;

public sealed class FeatureFlagEvaluator(IFeatureFlagRepository featureFlagRepository)
{
    public async Task<IReadOnlyList<string>> EvaluateAsync(TenantId tenantId, UserId userId, int tenantRolloutBucket, int userRolloutBucket, CancellationToken cancellationToken)
    {
        var allRows = await featureFlagRepository.GetAllRelevantRowsAsync(tenantId, userId, cancellationToken);
        var enabledFeatureFlags = new List<string>();

        var featureFlagDefinitions = SharedKernel.Domain.FeatureFlags.GetAll();

        // Sort feature flags so parents are evaluated before children
        var sortedFeatureFlagDefinitions = TopologicalSort(featureFlagDefinitions);

        var enabledFeatureFlagSet = new HashSet<string>();

        foreach (var featureFlagDefinition in sortedFeatureFlagDefinitions)
        {
            if (featureFlagDefinition.Scope == FeatureFlagScope.System) continue;

            var baseRow = allRows.FirstOrDefault(f => f.FeatureFlagKey == featureFlagDefinition.Key && f.TenantId is null && f.UserId is null);
            if (baseRow is null) continue;

            if (!IsActive(baseRow)) continue;

            if (featureFlagDefinition.ParentDependency is not null && !enabledFeatureFlagSet.Contains(featureFlagDefinition.ParentDependency)) continue;

            var isEnabled = featureFlagDefinition.Scope switch
            {
                FeatureFlagScope.Tenant => EvaluateTenantScope(featureFlagDefinition, baseRow, allRows, tenantId, tenantRolloutBucket),
                FeatureFlagScope.User => EvaluateUserScope(featureFlagDefinition, baseRow, allRows, tenantId, userId, userRolloutBucket),
                _ => false
            };

            if (!isEnabled) continue;

            enabledFeatureFlagSet.Add(featureFlagDefinition.Key);
            enabledFeatureFlags.Add(featureFlagDefinition.Key);
        }

        return enabledFeatureFlags;
    }

    private static bool EvaluateTenantScope(FeatureFlagDefinition featureFlagDefinition, FeatureFlag baseRow, FeatureFlag[] allRows, TenantId tenantId, int tenantRolloutBucket)
    {
        var tenantFeatureFlag = allRows.FirstOrDefault(f => f.FeatureFlagKey == featureFlagDefinition.Key && f.TenantId == tenantId && f.UserId is null);
        if (tenantFeatureFlag is not null)
        {
            return IsActive(tenantFeatureFlag);
        }

        if (featureFlagDefinition.IsAbTestEligible && baseRow.RolloutBucketStart is not null && baseRow.RolloutBucketEnd is not null)
        {
            return RolloutBucketHasher.IsInRolloutBucketRange(tenantRolloutBucket, baseRow.RolloutBucketStart.Value, baseRow.RolloutBucketEnd.Value);
        }

        return false;
    }

    private static bool EvaluateUserScope(FeatureFlagDefinition featureFlagDefinition, FeatureFlag baseRow, FeatureFlag[] allRows, TenantId tenantId, UserId userId, int userRolloutBucket)
    {
        var userFeatureFlag = allRows.FirstOrDefault(f => f.FeatureFlagKey == featureFlagDefinition.Key && f.TenantId == tenantId && f.UserId == userId);
        if (userFeatureFlag is not null)
        {
            return IsActive(userFeatureFlag);
        }

        if (featureFlagDefinition.IsAbTestEligible && baseRow.RolloutBucketStart is not null && baseRow.RolloutBucketEnd is not null)
        {
            return RolloutBucketHasher.IsInRolloutBucketRange(userRolloutBucket, baseRow.RolloutBucketStart.Value, baseRow.RolloutBucketEnd.Value);
        }

        return false;
    }

    private static bool IsActive(FeatureFlag featureFlag)
    {
        return featureFlag.EnabledAt is not null && (featureFlag.DisabledAt is null || featureFlag.EnabledAt > featureFlag.DisabledAt);
    }

    private static FeatureFlagDefinition[] TopologicalSort(FeatureFlagDefinition[] featureFlagDefinitions)
    {
        var result = new List<FeatureFlagDefinition>(featureFlagDefinitions.Length);

        // Add feature flags without parent dependencies first
        foreach (var featureFlagDefinition in featureFlagDefinitions)
        {
            if (featureFlagDefinition.ParentDependency is null)
            {
                result.Add(featureFlagDefinition);
            }
        }

        // Then add feature flags with parent dependencies
        foreach (var featureFlagDefinition in featureFlagDefinitions)
        {
            if (featureFlagDefinition.ParentDependency is not null)
            {
                result.Add(featureFlagDefinition);
            }
        }

        return result.ToArray();
    }
}
