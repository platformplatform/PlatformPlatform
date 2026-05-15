using Account.Features.FeatureFlags.Domain;
using SharedKernel.Domain;
using SharedKernel.FeatureFlags;

namespace Account.Features.FeatureFlags.Shared;

/// <summary>
///     Resolves the enabled feature flags for a (tenant, user) context. Precedence runs base-row-active
///     and parent-dependency as gates, then manual override > AB inclusion pin > rollout bucket > default
///     off. Pins are unconditional: AlwaysOn forces inclusion even at 0% rollout, NeverOn forces exclusion
///     even at 100%. Plan-gated flags participate as manual overrides because the Stripe pipeline writes
///     them as Source=Plan tenant rows. The four BackOffice query mirrors apply the same ordering.
/// </summary>
public sealed class FeatureFlagEvaluator(IFeatureFlagRepository featureFlagRepository)
{
    // The definitions source defaults to the reflected registry but is overridable for tests that need
    // to exercise specific topologies (e.g., parent-dependency gating) without contributing test-only
    // flags to the production registry.
    public Func<FeatureFlagDefinition[]> DefinitionsProvider { get; init; } = SharedKernel.FeatureFlags.FeatureFlags.GetAll;

    public async Task<IReadOnlyList<string>> EvaluateAsync(
        TenantId tenantId,
        UserId userId,
        int tenantRolloutBucket,
        int? userRolloutBucket,
        AbInclusionPin? tenantAbInclusionPin,
        AbInclusionPin? userAbInclusionPin,
        CancellationToken cancellationToken
    )
    {
        var allRows = await featureFlagRepository.GetAllRelevantRowsAsync(tenantId, userId, cancellationToken);
        var enabledFeatureFlags = new List<string>();

        var definitions = DefinitionsProvider();

        // Sort feature flags so parents are evaluated before children
        var sorted = SortByParentDependencyFirst(definitions);

        var enabledFeatureFlagSet = new HashSet<string>();

        foreach (var definition in sorted)
        {
            if (definition.Scope == FeatureFlagScope.System) continue;

            var baseRow = allRows.FirstOrDefault(f => f.FlagKey == definition.Key && f.TenantId is null && f.UserId is null);
            if (baseRow is null) continue;

            if (!baseRow.IsActive) continue;

            if (definition.ParentDependency is not null && !enabledFeatureFlagSet.Contains(definition.ParentDependency)) continue;

            var isEnabled = definition.Scope switch
            {
                FeatureFlagScope.Tenant => EvaluateTenantScope(definition, baseRow, allRows, tenantId, tenantRolloutBucket, tenantAbInclusionPin),
                FeatureFlagScope.User => EvaluateUserScope(definition, baseRow, allRows, tenantId, userId, userRolloutBucket, userAbInclusionPin),
                _ => false
            };

            if (!isEnabled) continue;

            enabledFeatureFlagSet.Add(definition.Key);
            enabledFeatureFlags.Add(definition.Key);
        }

        return enabledFeatureFlags;
    }

    private static bool EvaluateTenantScope(FeatureFlagDefinition definition, FeatureFlag baseRow, FeatureFlag[] allRows, TenantId tenantId, int tenantRolloutBucket, AbInclusionPin? tenantAbInclusionPin)
    {
        var tenantOverride = allRows.FirstOrDefault(f => f.FlagKey == definition.Key && f.TenantId == tenantId && f.UserId is null);
        if (tenantOverride is not null) return tenantOverride.IsActive;

        if (!definition.IsAbTestEligible) return false;

        // Precedence (matches .claude/rules/backend/backend.md): manual override (above) > pin > rollout.
        // Pins are unconditional: AlwaysOn includes the tenant even when rollout is at 0%/unset, and
        // NeverOn excludes the tenant even at 100% rollout. This is what the project-level precedence
        // story documents and what admins expect from a pin labelled "always on".
        if (tenantAbInclusionPin is AbInclusionPin.AlwaysOn) return true;
        if (tenantAbInclusionPin is AbInclusionPin.NeverOn) return false;

        if (baseRow.BucketStart is null || baseRow.BucketEnd is null) return false;

        return RolloutBucketHasher.IsInRolloutBucketRange(tenantRolloutBucket, baseRow.BucketStart.Value, baseRow.BucketEnd.Value);
    }

    private static bool EvaluateUserScope(FeatureFlagDefinition definition, FeatureFlag baseRow, FeatureFlag[] allRows, TenantId tenantId, UserId userId, int? userRolloutBucket, AbInclusionPin? userAbInclusionPin)
    {
        var userOverride = allRows.FirstOrDefault(f => f.FlagKey == definition.Key && f.TenantId == tenantId && f.UserId == userId);
        if (userOverride is not null) return userOverride.IsActive;

        if (!definition.IsAbTestEligible) return false;

        if (userAbInclusionPin is AbInclusionPin.AlwaysOn) return true;
        if (userAbInclusionPin is AbInclusionPin.NeverOn) return false;

        if (baseRow.BucketStart is null || baseRow.BucketEnd is null) return false;
        if (userRolloutBucket is null) return false;

        return RolloutBucketHasher.IsInRolloutBucketRange(userRolloutBucket.Value, baseRow.BucketStart.Value, baseRow.BucketEnd.Value);
    }

    // Two-pass parent-first ordering: parentless flags first, then flags with a parent. This relies on
    // the one-level dependency invariant enforced by FeatureFlags.ValidateFlags — a parent can never
    // itself have a parent, so a single pass over each bucket is sufficient (no full topological sort
    // is needed).
    private static FeatureFlagDefinition[] SortByParentDependencyFirst(FeatureFlagDefinition[] definitions)
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
