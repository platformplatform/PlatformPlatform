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
public sealed record GetFeatureFlagTenantsQuery(
    string? Search = null,
    SubscriptionPlan[]? Plans = null,
    FeatureFlagAudienceState? State = null,
    bool? HasOverride = null,
    int PageOffset = 0,
    int PageSize = 25
) : IRequest<Result<GetFeatureFlagTenantsResponse>>
{
    [JsonIgnore] // Removes from API contract
    public string FlagKey { get; init; } = null!;

    public string? Search { get; } = Search?.Trim().ToLower();

    public SubscriptionPlan[] Plans { get; } = Plans ?? [];
}

[PublicAPI]
public sealed record GetFeatureFlagTenantsResponse(int TotalCount, int PageSize, int TotalPages, int CurrentPageOffset, FeatureFlagTenantInfo[] Tenants);

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
    FeatureFlagSource Source
);

public sealed class GetFeatureFlagTenantsValidator : AbstractValidator<GetFeatureFlagTenantsQuery>
{
    public GetFeatureFlagTenantsValidator()
    {
        RuleFor(x => x.FlagKey)
            .NotEmpty().WithMessage("Feature flag key must not be empty.")
            .Must(key => SharedKernel.FeatureFlags.FeatureFlags.Get(key) is not null).WithMessage("Feature flag key must exist in the registry.")
            .Must(key => SharedKernel.FeatureFlags.FeatureFlags.Get(key)?.Scope == FeatureFlagScope.Tenant).WithMessage("Feature flag must have tenant scope.");

        RuleFor(x => x.Search).MaximumLength(100).WithMessage("The search term must be at most 100 characters.");
        RuleFor(x => x.Plans.Length).LessThanOrEqualTo(10).WithMessage("Plans filter must contain no more than 10 values.");
        // Plan-gated flag detail page requests every tenant in one shot (PLAN_TENANT_LIST_CAP = 1000) so it can
        // group them by plan without pagination. Other call sites still use the default 25.
        RuleFor(x => x.PageSize).InclusiveBetween(1, 1000).WithMessage("Page size must be between 1 and 1000.");
        RuleFor(x => x.PageOffset).GreaterThanOrEqualTo(0).WithMessage("Page offset must be greater than or equal to 0.");
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

        // Load all tenants then narrow in memory: search must match tenant name OR owner email, which can't be expressed
        // by the existing SearchAllTenantsAsync (name + id only). Tenant counts in back-office are bounded.
        var tenants = await tenantRepository.GetAllUnfilteredAsync(cancellationToken);

        if (query.Plans.Length > 0)
        {
            tenants = tenants.Where(t => query.Plans.Contains(t.Plan)).ToArray();
        }

        var tenantIds = tenants.Select(t => t.Id).ToArray();

        var subscriptions = tenantIds.Length == 0
            ? []
            : await subscriptionRepository.GetByTenantIdsUnfilteredAsync(tenantIds, cancellationToken);
        var subscriptionsByTenantId = subscriptions.ToDictionary(s => s.TenantId);

        var ownerByTenantId = tenantIds.Length == 0
            ? new Dictionary<TenantId, User>()
            : await userRepository.GetFirstOwnerByTenantIdsUnfilteredAsync(tenantIds, cancellationToken);

        if (!string.IsNullOrEmpty(query.Search))
        {
            tenants = tenants.Where(t =>
                t.Name.ToLowerInvariant().Contains(query.Search) ||
                (ownerByTenantId.TryGetValue(t.Id, out var owner) && owner.Email.Contains(query.Search))
            ).ToArray();
        }

        var tenantOverrides = await featureFlagRepository.GetTenantOverridesForFlagAsync(query.FlagKey, cancellationToken);
        var overridesByTenantId = tenantOverrides.ToDictionary(f => f.TenantId!);

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

        var filtered = query.State switch
        {
            FeatureFlagAudienceState.Enabled => featureFlagTenants.Where(t => t.IsEnabled).ToArray(),
            FeatureFlagAudienceState.Disabled => featureFlagTenants.Where(t => !t.IsEnabled).ToArray(),
            _ => featureFlagTenants
        };

        if (query.HasOverride == true)
        {
            filtered = filtered.Where(t => t.Source == FeatureFlagSource.Manual).ToArray();
        }

        var ordered = filtered.OrderBy(t => t.Name).ToArray();

        var totalCount = ordered.Length;
        var totalPages = totalCount == 0 ? 0 : (totalCount - 1) / query.PageSize + 1;
        if (query.PageOffset > 0 && query.PageOffset >= totalPages)
        {
            return Result<GetFeatureFlagTenantsResponse>.BadRequest($"The page offset '{query.PageOffset}' is greater than the total number of pages.");
        }

        var paged = ordered.Skip(query.PageOffset * query.PageSize).Take(query.PageSize).ToArray();

        return new GetFeatureFlagTenantsResponse(totalCount, query.PageSize, totalPages, query.PageOffset, paged);
    }

    private static (bool IsEnabled, FeatureFlagSource Source) EvaluateOverride(
        FeatureFlagDefinition definition,
        FeatureFlag? baseRow,
        Dictionary<TenantId, FeatureFlag> overridesByTenantId,
        Tenant tenant
    )
    {
        if (overridesByTenantId.TryGetValue(tenant.Id, out var tenantOverride))
        {
            var isEnabled = tenantOverride.EnabledAt is not null && (tenantOverride.DisabledAt is null || tenantOverride.EnabledAt > tenantOverride.DisabledAt);
            // The override row's Source column distinguishes a manual admin toggle from a plan-driven row.
            var source = tenantOverride.Source == FeatureFlagSource.Plan ? FeatureFlagSource.Plan : FeatureFlagSource.Manual;
            return (isEnabled, source);
        }

        if (definition.IsAbTestEligible && baseRow?.BucketStart is not null && baseRow.BucketEnd is not null)
        {
            var isInRange = RolloutBucketHasher.IsInRolloutBucketRange(tenant.RolloutBucket, baseRow.BucketStart.Value, baseRow.BucketEnd.Value);
            return (isInRange, FeatureFlagSource.AbRollout);
        }

        return (false, FeatureFlagSource.Default);
    }
}
