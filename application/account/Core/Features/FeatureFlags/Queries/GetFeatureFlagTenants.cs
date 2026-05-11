using Account.Features.FeatureFlags.Domain;
using Account.Features.Subscriptions.Domain;
using Account.Features.Tenants.BackOffice.Queries;
using Account.Features.Tenants.Domain;
using Account.Features.Users.Domain;
using FluentValidation;
using JetBrains.Annotations;
using Mapster;
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

// Field names mirror TenantSummary so Mapster's convention-based mapping covers the shared subset. Override fields
// (RolloutBucket, IsEnabled, Source) come from the feature-flag evaluation and are applied via `with` on top of Adapt.
[PublicAPI]
public sealed record FeatureFlagTenantInfo(
    TenantId Id,
    string Name,
    string? LogoUrl,
    SubscriptionPlan Plan,
    decimal? MonthlyRecurringRevenue,
    decimal? ScheduledPriceAmount,
    string? Currency,
    DateTimeOffset? RenewalDate,
    PlannedSubscriptionChange? PlannedChange,
    bool HasEverSubscribed,
    string? Country,
    DateTimeOffset CreatedAt,
    TenantOwnerSummary? Owner,
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

public sealed class GetFeatureFlagTenantsHandler(
    IFeatureFlagRepository featureFlagRepository,
    ITenantRepository tenantRepository,
    ISubscriptionRepository subscriptionRepository,
    IUserRepository userRepository
) : IRequestHandler<GetFeatureFlagTenantsQuery, Result<GetFeatureFlagTenantsResponse>>
{
    public async Task<Result<GetFeatureFlagTenantsResponse>> Handle(GetFeatureFlagTenantsQuery query, CancellationToken cancellationToken)
    {
        var definition = SharedKernel.FeatureFlags.FeatureFlags.Get(query.FlagKey);
        if (definition is null) return Result<GetFeatureFlagTenantsResponse>.NotFound($"Feature flag with key '{query.FlagKey}' not found.");

        var tenants = await tenantRepository.GetAllUnfilteredAsync(cancellationToken);
        var tenantIds = tenants.Select(t => t.Id).ToArray();

        var subscriptions = tenantIds.Length == 0
            ? []
            : await subscriptionRepository.GetByTenantIdsUnfilteredAsync(tenantIds, cancellationToken);
        var subscriptionsByTenantId = subscriptions.ToDictionary(s => s.TenantId);

        var ownerByTenantId = tenantIds.Length == 0
            ? new Dictionary<TenantId, User>()
            : await userRepository.GetFirstOwnerByTenantIdsUnfilteredAsync(tenantIds, cancellationToken);

        var tenantOverrides = await featureFlagRepository.GetTenantOverridesForFlagAsync(query.FlagKey, cancellationToken);
        var overridesByTenantId = tenantOverrides.ToDictionary(f => f.TenantId!.Value);

        var baseRow = await featureFlagRepository.GetByKeyAndScopeAsync(query.FlagKey, null, null, cancellationToken);

        var featureFlagTenants = tenants.Select(tenant =>
            {
                var summary = TenantSummary.FromAggregate(
                    tenant,
                    subscriptionsByTenantId.GetValueOrDefault(tenant.Id),
                    ownerByTenantId.GetValueOrDefault(tenant.Id)
                );

                var (isEnabled, source) = EvaluateOverride(definition, baseRow, overridesByTenantId, tenant);

                return summary.Adapt<FeatureFlagTenantInfo>() with
                {
                    RolloutBucket = tenant.RolloutBucket, IsEnabled = isEnabled, Source = source
                };
            }
        ).ToArray();

        return new GetFeatureFlagTenantsResponse(featureFlagTenants);
    }

    private static (bool IsEnabled, string Source) EvaluateOverride(
        FeatureFlagDefinition definition,
        FeatureFlag? baseRow,
        Dictionary<long, FeatureFlag> overridesByTenantId,
        Tenant tenant
    )
    {
        if (overridesByTenantId.TryGetValue(tenant.Id.Value, out var tenantOverride))
        {
            var isEnabled = tenantOverride.EnabledAt is not null && (tenantOverride.DisabledAt is null || tenantOverride.EnabledAt > tenantOverride.DisabledAt);
            // The override row's Source column distinguishes a manual admin toggle from a plan-driven row.
            var source = tenantOverride.Source == FeatureFlagSource.Plan ? "plan" : "manual_override";
            return (isEnabled, source);
        }

        if (definition.IsAbTestEligible && baseRow?.BucketStart is not null && baseRow.BucketEnd is not null)
        {
            var isInRange = RolloutBucketHasher.IsInRolloutBucketRange(tenant.RolloutBucket, baseRow.BucketStart.Value, baseRow.BucketEnd.Value);
            return (isInRange, "ab_rollout");
        }

        return (false, "default");
    }
}
