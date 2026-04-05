using Account.Features.FeatureFlags.Domain;
using SharedKernel.FeatureFlags;

namespace Account.Features.FeatureFlags.Shared;

public sealed class FeatureFlagEvaluator(IFeatureFlagRepository featureFlagRepository)
{
    public async Task<IReadOnlyList<string>> EvaluateAsync(long tenantId, string userId, int tenantRolloutBucket, int? userRolloutBucket, CancellationToken cancellationToken)
    {
        var allRows = await featureFlagRepository.GetAllRelevantRowsAsync(tenantId, userId, cancellationToken);
        var enabledFeatureFlags = new List<string>();

        var definitions = SharedKernel.FeatureFlags.FeatureFlags.GetAll();

        // Sort feature flags so parents are evaluated before children
        var sorted = TopologicalSort(definitions);

        var enabledFeatureFlagSet = new HashSet<string>();

        foreach (var definition in sorted)
        {
            if (definition.Scope == FeatureFlagScope.System) continue;

            var baseRow = allRows.FirstOrDefault(f => f.FlagKey == definition.Key && f.TenantId is null && f.UserId is null);
            if (baseRow is null) continue;

            if (!IsActive(baseRow)) continue;

            if (definition.ParentDependency is not null && !enabledFeatureFlagSet.Contains(definition.ParentDependency)) continue;

            var isEnabled = definition.Scope switch
            {
                FeatureFlagScope.Tenant => EvaluateTenantScope(definition, baseRow, allRows, tenantId, tenantRolloutBucket),
                FeatureFlagScope.User => EvaluateUserScope(definition, baseRow, allRows, tenantId, userId, userRolloutBucket),
                _ => false
            };

            if (!isEnabled) continue;

            enabledFeatureFlagSet.Add(definition.Key);
            enabledFeatureFlags.Add(definition.Key);
        }

        return enabledFeatureFlags;
    }

    private static bool EvaluateTenantScope(FeatureFlagDefinition definition, FeatureFlag baseRow, FeatureFlag[] allRows, long tenantId, int tenantRolloutBucket)
    {
        var tenantOverride = allRows.FirstOrDefault(f => f.FlagKey == definition.Key && f.TenantId == tenantId && f.UserId is null);
        if (tenantOverride is not null)
        {
            return IsActive(tenantOverride);
        }

        if (definition.IsAbTestEligible && baseRow.BucketStart is not null && baseRow.BucketEnd is not null)
        {
            return RolloutBucketHasher.IsInRolloutBucketRange(tenantRolloutBucket, baseRow.BucketStart.Value, baseRow.BucketEnd.Value);
        }

        return false;
    }

    private static bool EvaluateUserScope(FeatureFlagDefinition definition, FeatureFlag baseRow, FeatureFlag[] allRows, long tenantId, string userId, int? userRolloutBucket)
    {
        if (string.IsNullOrEmpty(userId)) return false;

        var userOverride = allRows.FirstOrDefault(f => f.FlagKey == definition.Key && f.TenantId == tenantId && f.UserId == userId);
        if (userOverride is not null)
        {
            return IsActive(userOverride);
        }

        if (definition.IsAbTestEligible && userRolloutBucket is not null && baseRow.BucketStart is not null && baseRow.BucketEnd is not null)
        {
            return RolloutBucketHasher.IsInRolloutBucketRange(userRolloutBucket.Value, baseRow.BucketStart.Value, baseRow.BucketEnd.Value);
        }

        return false;
    }

    private static bool IsActive(FeatureFlag featureFlag)
    {
        return featureFlag.EnabledAt is not null && (featureFlag.DisabledAt is null || featureFlag.EnabledAt > featureFlag.DisabledAt);
    }

    private static FeatureFlagDefinition[] TopologicalSort(FeatureFlagDefinition[] definitions)
    {
        var result = new List<FeatureFlagDefinition>(definitions.Length);

        // Add feature flags without parent dependencies first
        foreach (var definition in definitions)
        {
            if (definition.ParentDependency is null)
            {
                result.Add(definition);
            }
        }

        // Then add feature flags with parent dependencies
        foreach (var definition in definitions)
        {
            if (definition.ParentDependency is not null)
            {
                result.Add(definition);
            }
        }

        return result.ToArray();
    }
}
