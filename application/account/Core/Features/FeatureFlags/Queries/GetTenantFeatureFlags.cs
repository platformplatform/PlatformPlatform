using Account.Features.FeatureFlags.Domain;
using Account.Features.Tenants.Domain;
using JetBrains.Annotations;
using SharedKernel.Cqrs;
using SharedKernel.Domain;
using SharedKernel.FeatureFlags;

namespace Account.Features.FeatureFlags.Queries;

[PublicAPI]
public sealed record GetTenantFeatureFlagsQuery : IRequest<Result<GetTenantFeatureFlagsResponse>>
{
    [JsonIgnore] // Removes from API contract
    public TenantId TenantId { get; init; } = null!;
}

[PublicAPI]
public sealed record GetTenantFeatureFlagsResponse(TenantFeatureFlagInfo[] Flags);

[PublicAPI]
public sealed record TenantFeatureFlagInfo(
    string FlagKey,
    FeatureFlagScope Scope,
    string Description,
    string? RequiredPlan,
    bool IsAbTestEligible,
    int? BucketStart,
    int? BucketEnd,
    int? RolloutPercentage,
    bool IsEnabled,
    FeatureFlagSource Source,
    bool IsBaseRowActive,
    int RolloutBucket,
    int? InclusionThresholdPercentage,
    bool DefaultEnabled,
    AbInclusionPin? TenantAbInclusionPin
);

public sealed class GetTenantFeatureFlagsHandler(IFeatureFlagRepository featureFlagRepository, ITenantRepository tenantRepository)
    : IRequestHandler<GetTenantFeatureFlagsQuery, Result<GetTenantFeatureFlagsResponse>>
{
    public async Task<Result<GetTenantFeatureFlagsResponse>> Handle(GetTenantFeatureFlagsQuery query, CancellationToken cancellationToken)
    {
        var tenant = await tenantRepository.GetByIdUnfilteredAsync(query.TenantId, cancellationToken);
        if (tenant is null) return Result<GetTenantFeatureFlagsResponse>.NotFound($"Tenant with ID '{query.TenantId}' not found.");

        var tenantScopedDefinitions = SharedKernel.FeatureFlags.FeatureFlags.GetAll()
            .Where(f => f.Scope == FeatureFlagScope.Tenant)
            .ToArray();

        var allRows = await featureFlagRepository.GetTenantScopedRowsAsync(tenant.Id, cancellationToken);
        var baseRowsByKey = allRows.Where(r => r.TenantId is null).ToDictionary(r => r.FlagKey);
        var tenantOverridesByKey = allRows.Where(r => r.TenantId == tenant.Id).ToDictionary(r => r.FlagKey);

        var flags = tenantScopedDefinitions
            .Select(definition => Evaluate(definition, baseRowsByKey, tenantOverridesByKey, tenant.RolloutBucket, tenant.AbInclusionPin))
            .ToArray();

        return new GetTenantFeatureFlagsResponse(flags);
    }

    private static TenantFeatureFlagInfo Evaluate(
        FeatureFlagDefinition definition,
        Dictionary<string, FeatureFlag> baseRowsByKey,
        Dictionary<string, FeatureFlag> tenantOverridesByKey,
        int tenantRolloutBucket,
        AbInclusionPin? abInclusionPin
    )
    {
        baseRowsByKey.TryGetValue(definition.Key, out var baseRow);
        var isBaseRowActive = baseRow?.IsActive == true;
        tenantOverridesByKey.TryGetValue(definition.Key, out var tenantOverride);

        bool isEnabled;
        FeatureFlagSource source;

        if (tenantOverride is not null)
        {
            isEnabled = tenantOverride.IsActive;
            // The row's Source column is authoritative — a manually-toggled plan-gated flag must still surface as
            // Manual so admins see they overrode the plan-driven default, rather than the plan granting it.
            source = tenantOverride.Source == FeatureFlagSource.Plan ? FeatureFlagSource.Plan : FeatureFlagSource.Manual;
        }
        else if (definition.IsAbTestEligible && baseRow?.BucketStart is not null && baseRow.BucketEnd is not null)
        {
            // AlwaysOn behaves as if the tenant were the first slot in the rollout sequence (threshold 1%);
            // NeverOn behaves as if it were the last slot (threshold 100%). Mirrors FeatureFlagEvaluator.
            var effectiveBucket = abInclusionPin switch
            {
                AbInclusionPin.AlwaysOn => baseRow.BucketStart.Value,
                AbInclusionPin.NeverOn => (baseRow.BucketStart.Value - 1 + 100) % 100,
                _ => tenantRolloutBucket
            };
            isEnabled = isBaseRowActive
                        && RolloutBucketHasher.IsInRolloutBucketRange(effectiveBucket, baseRow.BucketStart.Value, baseRow.BucketEnd.Value);
            source = FeatureFlagSource.AbRollout;
        }
        else
        {
            isEnabled = false;
            source = FeatureFlagSource.Default;
        }

        var defaultEnabled = ComputeDefaultEnabled(definition, baseRow, isBaseRowActive, tenantRolloutBucket, abInclusionPin);
        var inclusionThresholdPercentage = ComputeInclusionThresholdPercentage(definition, tenantRolloutBucket, abInclusionPin);

        return new TenantFeatureFlagInfo(
            definition.Key,
            definition.Scope,
            definition.Description,
            definition.RequiredPlan?.ToString(),
            definition.IsAbTestEligible,
            baseRow?.BucketStart,
            baseRow?.BucketEnd,
            ComputeRolloutPercentage(baseRow?.BucketStart, baseRow?.BucketEnd),
            isEnabled,
            source,
            isBaseRowActive,
            tenantRolloutBucket,
            inclusionThresholdPercentage,
            defaultEnabled,
            abInclusionPin
        );
    }

    private static bool ComputeDefaultEnabled(FeatureFlagDefinition definition, FeatureFlag? baseRow, bool isBaseRowActive, int rolloutBucket, AbInclusionPin? abInclusionPin)
    {
        if (!isBaseRowActive) return false;
        if (!definition.IsAbTestEligible) return false;
        if (baseRow?.BucketStart is null || baseRow.BucketEnd is null) return false;
        var effectiveBucket = abInclusionPin switch
        {
            AbInclusionPin.AlwaysOn => baseRow.BucketStart.Value,
            AbInclusionPin.NeverOn => (baseRow.BucketStart.Value - 1 + 100) % 100,
            _ => rolloutBucket
        };
        return RolloutBucketHasher.IsInRolloutBucketRange(effectiveBucket, baseRow.BucketStart.Value, baseRow.BucketEnd.Value);
    }

    private static int? ComputeInclusionThresholdPercentage(FeatureFlagDefinition definition, int rolloutBucket, AbInclusionPin? abInclusionPin)
    {
        if (!definition.IsAbTestEligible) return null;
        if (abInclusionPin is AbInclusionPin.AlwaysOn) return 1;
        if (abInclusionPin is AbInclusionPin.NeverOn) return 100;
        return RolloutBucketHasher.ComputeInclusionThresholdPercentage(rolloutBucket, definition.Key);
    }

    private static int? ComputeRolloutPercentage(int? bucketStart, int? bucketEnd)
    {
        return RolloutBucketHasher.ComputeRolloutPercentage(bucketStart, bucketEnd);
    }
}
