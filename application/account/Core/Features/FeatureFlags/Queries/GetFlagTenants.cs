using Account.Features.FeatureFlags.Domain;
using Account.Features.Tenants.Domain;
using FluentValidation;
using JetBrains.Annotations;
using SharedKernel.Cqrs;
using SharedKernel.Domain;
using SharedKernel.FeatureFlags;

namespace Account.Features.FeatureFlags.Queries;

[PublicAPI]
public sealed record GetFlagTenantsQuery : IRequest<Result<GetFlagTenantsResponse>>
{
    [JsonIgnore] // Removes from API contract
    public string FlagKey { get; init; } = null!;
}

[PublicAPI]
public sealed record GetFlagTenantsResponse(FlagTenantInfo[] Tenants);

[PublicAPI]
public sealed record FlagTenantInfo(
    TenantId TenantId,
    string TenantName,
    string Plan,
    int RolloutBucket,
    bool IsEnabled,
    string Source
);

public sealed class GetFlagTenantsValidator : AbstractValidator<GetFlagTenantsQuery>
{
    public GetFlagTenantsValidator()
    {
        RuleFor(x => x.FlagKey)
            .NotEmpty().WithMessage("Flag key must not be empty.")
            .Must(key => SharedKernel.FeatureFlags.FeatureFlags.Get(key) is not null).WithMessage("Flag key must exist in the registry.")
            .Must(key => SharedKernel.FeatureFlags.FeatureFlags.Get(key)?.Scope == FeatureFlagScope.Tenant).WithMessage("Flag must have tenant scope.");
    }
}

public sealed class GetFlagTenantsHandler(IFeatureFlagRepository featureFlagRepository, ITenantRepository tenantRepository)
    : IRequestHandler<GetFlagTenantsQuery, Result<GetFlagTenantsResponse>>
{
    public async Task<Result<GetFlagTenantsResponse>> Handle(GetFlagTenantsQuery query, CancellationToken cancellationToken)
    {
        var definition = SharedKernel.FeatureFlags.FeatureFlags.Get(query.FlagKey);
        if (definition is null) return Result<GetFlagTenantsResponse>.NotFound($"Feature flag with key '{query.FlagKey}' not found.");

        var tenants = await tenantRepository.GetAllUnfilteredAsync(cancellationToken);
        var tenantOverrides = await featureFlagRepository.GetTenantOverridesForFlagAsync(query.FlagKey, cancellationToken);
        var overridesByTenantId = tenantOverrides.ToDictionary(f => f.TenantId!.Value);

        var baseRow = await featureFlagRepository.GetByKeyAndScopeAsync(query.FlagKey, null, null, cancellationToken);

        var flagTenants = tenants.Select(tenant =>
            {
                if (overridesByTenantId.TryGetValue(tenant.Id.Value, out var tenantOverride))
                {
                    var isEnabled = tenantOverride.EnabledAt is not null && (tenantOverride.DisabledAt is null || tenantOverride.EnabledAt > tenantOverride.DisabledAt);
                    return new FlagTenantInfo(tenant.Id, tenant.Name, tenant.Plan.ToString(), tenant.RolloutBucket, isEnabled, "manual_override");
                }

                if (definition.IsAbTestEligible && baseRow?.BucketStart is not null && baseRow.BucketEnd is not null)
                {
                    var isInRange = RolloutBucketHasher.IsInBucketRange(tenant.RolloutBucket, baseRow.BucketStart.Value, baseRow.BucketEnd.Value);
                    return new FlagTenantInfo(tenant.Id, tenant.Name, tenant.Plan.ToString(), tenant.RolloutBucket, isInRange, "ab_rollout");
                }

                return new FlagTenantInfo(tenant.Id, tenant.Name, tenant.Plan.ToString(), tenant.RolloutBucket, false, "default");
            }
        ).ToArray();

        return new GetFlagTenantsResponse(flagTenants);
    }
}
