using Account.Features.FeatureFlags.Domain;
using Account.Features.Tenants.Domain;
using FluentValidation;
using JetBrains.Annotations;
using SharedKernel.Cqrs;
using SharedKernel.Domain;
using SharedKernel.FeatureFlags;

namespace Account.Features.FeatureFlags.Queries;

[PublicAPI]
public sealed record GetFeatureFlagTenantsQuery : IRequest<Result<GetFeatureFlagTenantsResponse>>
{
    [JsonIgnore] // Removes from API contract
    public string FlagKey { get; init; } = null!;
}

[PublicAPI]
public sealed record GetFeatureFlagTenantsResponse(FeatureFlagTenantInfo[] Tenants);

[PublicAPI]
public sealed record FeatureFlagTenantInfo(
    TenantId TenantId,
    string TenantName,
    string Plan,
    int RolloutBucket,
    bool IsEnabled,
    string Source
);

public sealed class GetFeatureFlagTenantsValidator : AbstractValidator<GetFeatureFlagTenantsQuery>
{
    public GetFeatureFlagTenantsValidator()
    {
        RuleFor(x => x.FlagKey)
            .NotEmpty().WithMessage("Feature flag key must not be empty.")
            .Must(key => SharedKernel.FeatureFlags.FeatureFlags.Get(key) is not null).WithMessage("Feature flag key must exist in the registry.")
            .Must(key => SharedKernel.FeatureFlags.FeatureFlags.Get(key)?.Scope == FeatureFlagScope.Tenant).WithMessage("Feature flag must have tenant scope.");
    }
}

public sealed class GetFeatureFlagTenantsHandler(IFeatureFlagRepository featureFlagRepository, ITenantRepository tenantRepository)
    : IRequestHandler<GetFeatureFlagTenantsQuery, Result<GetFeatureFlagTenantsResponse>>
{
    public async Task<Result<GetFeatureFlagTenantsResponse>> Handle(GetFeatureFlagTenantsQuery query, CancellationToken cancellationToken)
    {
        var definition = SharedKernel.FeatureFlags.FeatureFlags.Get(query.FlagKey);
        if (definition is null) return Result<GetFeatureFlagTenantsResponse>.NotFound($"Feature flag with key '{query.FlagKey}' not found.");

        var tenants = await tenantRepository.GetAllUnfilteredAsync(cancellationToken);
        var tenantOverrides = await featureFlagRepository.GetTenantOverridesForFlagAsync(query.FlagKey, cancellationToken);
        var overridesByTenantId = tenantOverrides.ToDictionary(f => f.TenantId!.Value);

        var baseRow = await featureFlagRepository.GetByKeyAndScopeAsync(query.FlagKey, null, null, cancellationToken);

        var featureFlagTenants = tenants.Select(tenant =>
            {
                if (overridesByTenantId.TryGetValue(tenant.Id.Value, out var tenantOverride))
                {
                    var isEnabled = tenantOverride.EnabledAt is not null && (tenantOverride.DisabledAt is null || tenantOverride.EnabledAt > tenantOverride.DisabledAt);
                    return new FeatureFlagTenantInfo(tenant.Id, tenant.Name, tenant.Plan.ToString(), tenant.RolloutBucket, isEnabled, "manual_override");
                }

                if (definition.IsAbTestEligible && baseRow?.BucketStart is not null && baseRow.BucketEnd is not null)
                {
                    var isInRange = RolloutBucketHasher.IsInRolloutBucketRange(tenant.RolloutBucket, baseRow.BucketStart.Value, baseRow.BucketEnd.Value);
                    return new FeatureFlagTenantInfo(tenant.Id, tenant.Name, tenant.Plan.ToString(), tenant.RolloutBucket, isInRange, "ab_rollout");
                }

                return new FeatureFlagTenantInfo(tenant.Id, tenant.Name, tenant.Plan.ToString(), tenant.RolloutBucket, false, "default");
            }
        ).ToArray();

        return new GetFeatureFlagTenantsResponse(featureFlagTenants);
    }
}
