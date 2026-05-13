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
    int RolloutBucket
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
            .Select(definition => Evaluate(definition, baseRowsByKey, tenantOverridesByKey, tenant.RolloutBucket))
            .ToArray();

        return new GetTenantFeatureFlagsResponse(flags);
    }

    private static TenantFeatureFlagInfo Evaluate(
        FeatureFlagDefinition definition,
        Dictionary<string, FeatureFlag> baseRowsByKey,
        Dictionary<string, FeatureFlag> tenantOverridesByKey,
        int tenantRolloutBucket
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
            isEnabled = isBaseRowActive
                        && RolloutBucketHasher.IsInRolloutBucketRange(tenantRolloutBucket, baseRow.BucketStart.Value, baseRow.BucketEnd.Value);
            source = FeatureFlagSource.AbRollout;
        }
        else
        {
            isEnabled = false;
            source = FeatureFlagSource.Default;
        }

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
            tenantRolloutBucket
        );
    }

    private static int? ComputeRolloutPercentage(int? bucketStart, int? bucketEnd)
    {
        if (bucketStart is null || bucketEnd is null) return null;

        // 100% rollout uses reserved range 0-100
        if (bucketStart == 0 && bucketEnd == 100) return 100;

        if (bucketStart <= bucketEnd) return bucketEnd.Value - bucketStart.Value + 1;

        // Wrap-around case within 1-99 range
        return 99 - bucketStart.Value + 1 + bucketEnd.Value;
    }
}
