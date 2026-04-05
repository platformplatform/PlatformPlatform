using Account.Features.FeatureFlags.Domain;
using Account.Features.Tenants.Domain;
using FluentValidation;
using JetBrains.Annotations;
using SharedKernel.Cqrs;
using SharedKernel.Domain;

namespace Account.Features.FeatureFlags.Queries;

[PublicAPI]
public sealed record GetFeatureFlagTenantsQuery : IRequest<Result<GetFeatureFlagTenantsResponse>>
{
    [JsonIgnore] // Removes from API contract
    public FeatureFlagKey FeatureFlagKey { get; init; } = null!;
}

[PublicAPI]
public sealed record GetFeatureFlagTenantsResponse(FeatureFlagTenantInfo[] Tenants);

[PublicAPI]
public sealed record FeatureFlagTenantInfo(
    TenantId TenantId,
    string TenantName,
    SubscriptionPlan Plan,
    int RolloutBucket,
    bool IsEnabled,
    FeatureFlagOverrideSource Source
);

public sealed class GetFeatureFlagTenantsValidator : AbstractValidator<GetFeatureFlagTenantsQuery>
{
    public GetFeatureFlagTenantsValidator()
    {
        RuleFor(x => x.FeatureFlagKey)
            .NotEmpty().WithMessage("Feature flag key must not be empty.")
            .Must(key => SharedKernel.Domain.FeatureFlags.Get(key) is not null).WithMessage("Feature flag key must exist in the registry.")
            .Must(key => SharedKernel.Domain.FeatureFlags.Get(key)?.Scope == FeatureFlagScope.Tenant).WithMessage("Feature flag must have tenant scope.");
    }
}

public sealed class GetFeatureFlagTenantsHandler(IFeatureFlagRepository featureFlagRepository, ITenantRepository tenantRepository)
    : IRequestHandler<GetFeatureFlagTenantsQuery, Result<GetFeatureFlagTenantsResponse>>
{
    public async Task<Result<GetFeatureFlagTenantsResponse>> Handle(GetFeatureFlagTenantsQuery query, CancellationToken cancellationToken)
    {
        var featureFlagDefinition = SharedKernel.Domain.FeatureFlags.Get(query.FeatureFlagKey);
        if (featureFlagDefinition is null) return Result<GetFeatureFlagTenantsResponse>.NotFound($"Feature flag with key '{query.FeatureFlagKey}' not found.");

        var tenants = await tenantRepository.GetAllUnfilteredAsync(cancellationToken);
        var tenantOverrides = await featureFlagRepository.GetTenantOverridesForFlagAsync(query.FeatureFlagKey, cancellationToken);
        var featureFlagsByTenantId = tenantOverrides.ToDictionary(f => f.TenantId!);

        var baseFeatureFlag = await featureFlagRepository.GetBaseFeatureFlagByKeyAsync(query.FeatureFlagKey, cancellationToken);

        var featureFlagTenants = tenants.Select(tenant =>
            {
                if (featureFlagsByTenantId.TryGetValue(tenant.Id, out var tenantFeatureFlag))
                {
                    var isEnabled = tenantFeatureFlag.EnabledAt is not null && (tenantFeatureFlag.DisabledAt is null || tenantFeatureFlag.EnabledAt > tenantFeatureFlag.DisabledAt);
                    return new FeatureFlagTenantInfo(tenant.Id, tenant.Name, tenant.Plan, tenant.RolloutBucket, isEnabled, FeatureFlagOverrideSource.ManualOverride);
                }

                if (featureFlagDefinition.IsAbTestEligible && baseFeatureFlag?.RolloutBucketStart is not null && baseFeatureFlag.RolloutBucketEnd is not null)
                {
                    var isInRange = RolloutBucketHasher.IsInRolloutBucketRange(tenant.RolloutBucket, baseFeatureFlag.RolloutBucketStart.Value, baseFeatureFlag.RolloutBucketEnd.Value);
                    return new FeatureFlagTenantInfo(tenant.Id, tenant.Name, tenant.Plan, tenant.RolloutBucket, isInRange, FeatureFlagOverrideSource.AbRollout);
                }

                return new FeatureFlagTenantInfo(tenant.Id, tenant.Name, tenant.Plan, tenant.RolloutBucket, false, FeatureFlagOverrideSource.Default);
            }
        ).ToArray();

        return new GetFeatureFlagTenantsResponse(featureFlagTenants);
    }
}
