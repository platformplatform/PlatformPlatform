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
using SharedKernel.Persistence;

namespace Account.Features.FeatureFlags.Queries;

[PublicAPI]
public sealed record GetFeatureFlagTenantsQuery(
    string? Search = null,
    SubscriptionPlan[]? Plans = null,
    FeatureFlagAudienceState? State = null,
    bool? HasOverride = null,
    SortableFeatureFlagTenantProperties OrderBy = SortableFeatureFlagTenantProperties.Name,
    SortOrder SortOrder = SortOrder.Ascending,
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
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum SortableFeatureFlagTenantProperties
{
    Name,
    Plan,
    MonthlyRecurringRevenue,
    RenewalDate,
    IsEnabled,
    OverrideUpdatedAt,
    InclusionThresholdPercentage
}

[PublicAPI]
public sealed record GetFeatureFlagTenantsResponse(
    int TotalCount,
    int PageSize,
    int TotalPages,
    int CurrentPageOffset,
    int EnabledCount,
    int DisabledCount,
    int OverrideCount,
    FeatureFlagTenantInfo[] Tenants
);

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
    FeatureFlagSource Source,
    int? InclusionThresholdPercentage,
    bool DefaultEnabled,
    DateTimeOffset? OverrideEnabledAt,
    DateTimeOffset? OverrideDisabledAt,
    AbInclusionPin? TenantAbInclusionPin
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

                var defaultEnabled = ComputeDefaultEnabled(definition, baseRow, tenant.RolloutBucket, tenant.AbInclusionPin);
                var inclusionThresholdPercentage = ComputeInclusionThresholdPercentage(definition, tenant.RolloutBucket, tenant.AbInclusionPin, query.FlagKey);

                overridesByTenantId.TryGetValue(tenant.Id, out var tenantOverrideRow);

                return summary.Adapt<FeatureFlagTenantInfo>() with
                {
                    RolloutBucket = tenant.RolloutBucket,
                    IsEnabled = isEnabled,
                    Source = source,
                    InclusionThresholdPercentage = inclusionThresholdPercentage,
                    DefaultEnabled = defaultEnabled,
                    OverrideEnabledAt = tenantOverrideRow?.EnabledAt,
                    OverrideDisabledAt = tenantOverrideRow?.DisabledAt,
                    TenantAbInclusionPin = tenant.AbInclusionPin
                };
            }
        ).ToArray();

        // Aggregate stats reflect the population AFTER search/plans filtering but BEFORE state/has-override
        // filtering, so they describe "the addressable accounts for this flag" rather than the current view.
        var enabledCount = featureFlagTenants.Count(t => t.IsEnabled);
        var disabledCount = featureFlagTenants.Length - enabledCount;
        var overrideCount = featureFlagTenants.Count(t => t.Source == FeatureFlagSource.Manual);

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

        var ordered = Sort(filtered, query.OrderBy, query.SortOrder).ToArray();

        var totalCount = ordered.Length;
        var totalPages = totalCount == 0 ? 0 : (totalCount - 1) / query.PageSize + 1;
        if (query.PageOffset > 0 && query.PageOffset >= totalPages)
        {
            return Result<GetFeatureFlagTenantsResponse>.BadRequest($"The page offset '{query.PageOffset}' is greater than the total number of pages.");
        }

        var paged = ordered.Skip(query.PageOffset * query.PageSize).Take(query.PageSize).ToArray();

        return new GetFeatureFlagTenantsResponse(totalCount, query.PageSize, totalPages, query.PageOffset, enabledCount, disabledCount, overrideCount, paged);
    }

    // Stable tie-break by Id keeps paginated results deterministic when the primary key has duplicates.
    private static IEnumerable<FeatureFlagTenantInfo> Sort(FeatureFlagTenantInfo[] items, SortableFeatureFlagTenantProperties orderBy, SortOrder sortOrder)
    {
        return (orderBy, sortOrder) switch
        {
            (SortableFeatureFlagTenantProperties.Plan, SortOrder.Ascending) => items.OrderBy(t => t.Plan).ThenBy(t => t.Id.Value),
            (SortableFeatureFlagTenantProperties.Plan, _) => items.OrderByDescending(t => t.Plan).ThenBy(t => t.Id.Value),
            (SortableFeatureFlagTenantProperties.MonthlyRecurringRevenue, SortOrder.Ascending) => items.OrderBy(t => t.MonthlyRecurringRevenue ?? 0m).ThenBy(t => t.Id.Value),
            (SortableFeatureFlagTenantProperties.MonthlyRecurringRevenue, _) => items.OrderByDescending(t => t.MonthlyRecurringRevenue ?? 0m).ThenBy(t => t.Id.Value),
            (SortableFeatureFlagTenantProperties.RenewalDate, SortOrder.Ascending) => items.OrderBy(t => t.RenewalDate ?? DateTimeOffset.MaxValue).ThenBy(t => t.Id.Value),
            (SortableFeatureFlagTenantProperties.RenewalDate, _) => items.OrderByDescending(t => t.RenewalDate ?? DateTimeOffset.MinValue).ThenBy(t => t.Id.Value),
            (SortableFeatureFlagTenantProperties.IsEnabled, SortOrder.Ascending) => items.OrderBy(t => t.IsEnabled).ThenBy(t => t.Id.Value),
            (SortableFeatureFlagTenantProperties.IsEnabled, _) => items.OrderByDescending(t => t.IsEnabled).ThenBy(t => t.Id.Value),
            (SortableFeatureFlagTenantProperties.OverrideUpdatedAt, SortOrder.Ascending) => items.OrderBy(t => t.OverrideDisabledAt ?? t.OverrideEnabledAt ?? DateTimeOffset.MaxValue).ThenBy(t => t.Id.Value),
            (SortableFeatureFlagTenantProperties.OverrideUpdatedAt, _) => items.OrderByDescending(t => t.OverrideDisabledAt ?? t.OverrideEnabledAt ?? DateTimeOffset.MinValue).ThenBy(t => t.Id.Value),
            (SortableFeatureFlagTenantProperties.InclusionThresholdPercentage, SortOrder.Ascending) => items.OrderBy(t => t.InclusionThresholdPercentage ?? int.MaxValue).ThenBy(t => t.Id.Value),
            (SortableFeatureFlagTenantProperties.InclusionThresholdPercentage, _) => items.OrderByDescending(t => t.InclusionThresholdPercentage ?? int.MinValue).ThenBy(t => t.Id.Value),
            (SortableFeatureFlagTenantProperties.Name, SortOrder.Descending) => items.OrderByDescending(t => t.Name).ThenBy(t => t.Id.Value),
            _ => items.OrderBy(t => t.Name).ThenBy(t => t.Id.Value)
        };
    }

    // The state a tenant would have if no manual override existed. Pins are unconditional: AlwaysOn includes
    // the tenant regardless of rollout, NeverOn excludes them regardless of rollout. Mirrors FeatureFlagEvaluator.
    private static bool ComputeDefaultEnabled(FeatureFlagDefinition definition, FeatureFlag? baseRow, int rolloutBucket, AbInclusionPin? abInclusionPin)
    {
        if (baseRow is null || !baseRow.IsActive) return false;
        if (!definition.IsAbTestEligible) return false;
        return EvaluateAbRollout(baseRow, rolloutBucket, abInclusionPin);
    }

    private static bool EvaluateAbRollout(FeatureFlag baseRow, int rolloutBucket, AbInclusionPin? abInclusionPin)
    {
        if (abInclusionPin is AbInclusionPin.AlwaysOn) return true;
        if (abInclusionPin is AbInclusionPin.NeverOn) return false;
        if (baseRow.BucketStart is null || baseRow.BucketEnd is null) return false;
        return RolloutBucketHasher.IsInRolloutBucketRange(rolloutBucket, baseRow.BucketStart.Value, baseRow.BucketEnd.Value);
    }

    // Pinned entities bypass the rollout: AlwaysOn → 0 (always included) and NeverOn → null (never included
    // by rollout). Without a pin we fall back to the per-key threshold.
    private static int? ComputeInclusionThresholdPercentage(FeatureFlagDefinition definition, int rolloutBucket, AbInclusionPin? abInclusionPin, string flagKey)
    {
        if (!definition.IsAbTestEligible) return null;
        if (abInclusionPin is AbInclusionPin.AlwaysOn) return 0;
        if (abInclusionPin is AbInclusionPin.NeverOn) return null;
        return RolloutBucketHasher.ComputeInclusionThresholdPercentage(rolloutBucket, flagKey);
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
            // Gate on baseRow.IsActive so a globally-Deactivated kill-switch flag reports as disabled even when
            // an override row exists — matching the runtime FeatureFlagEvaluator.cs:48 short-circuit.
            var isOverrideActive = tenantOverride.EnabledAt is not null && (tenantOverride.DisabledAt is null || tenantOverride.EnabledAt > tenantOverride.DisabledAt);
            var isEnabled = baseRow is { IsActive: true } && isOverrideActive;
            // The override row's Source column distinguishes a manual admin toggle from a plan-driven row.
            var source = tenantOverride.Source == FeatureFlagSource.Plan ? FeatureFlagSource.Plan : FeatureFlagSource.Manual;
            return (isEnabled, source);
        }

        // Gate by baseRow.IsActive so a globally-deactivated flag never shows as Enabled in the
        // bulk admin view — matches FeatureFlagEvaluator.EvaluateAsync which skips entirely when
        // the base row is inactive.
        if (definition.IsAbTestEligible && baseRow is { IsActive: true })
        {
            return (EvaluateAbRollout(baseRow, tenant.RolloutBucket, tenant.AbInclusionPin), FeatureFlagSource.AbRollout);
        }

        return (false, FeatureFlagSource.Default);
    }
}
