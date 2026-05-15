using Account.Features.FeatureFlags.Domain;
using Account.Features.Subscriptions.Domain;
using Account.Features.Tenants.Domain;
using Account.Features.Users.BackOffice.Queries;
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
public sealed record GetFeatureFlagUsersQuery(
    string? Search = null,
    UserRole[]? Roles = null,
    FeatureFlagAudienceState? State = null,
    bool? HasOverride = null,
    SortableFeatureFlagUserProperties OrderBy = SortableFeatureFlagUserProperties.Name,
    SortOrder SortOrder = SortOrder.Ascending,
    int PageOffset = 0,
    int PageSize = 25
) : IRequest<Result<GetFeatureFlagUsersResponse>>
{
    [JsonIgnore] // Removes from API contract
    public string FlagKey { get; init; } = null!;

    public string? Search { get; } = Search?.Trim().ToLower();

    public UserRole[] Roles { get; } = Roles ?? [];
}

[PublicAPI]
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum SortableFeatureFlagUserProperties
{
    Name,
    TenantName,
    Role,
    LastSeenAt,
    IsEnabled,
    OverrideUpdatedAt,
    InclusionThresholdPercentage
}

[PublicAPI]
public sealed record GetFeatureFlagUsersResponse(
    int TotalCount,
    int PageSize,
    int TotalPages,
    int CurrentPageOffset,
    int EnabledCount,
    int DisabledCount,
    int OverrideCount,
    FeatureFlagUserInfo[] Users
);

// Field names mirror the User aggregate so Mapster's convention-based mapping covers the user subset. Tenant-derived
// fields (TenantName, TenantPlan) and override fields (RolloutBucket, IsEnabled, Source) are applied via `with`.
[PublicAPI]
public sealed record FeatureFlagUserInfo(
    UserId Id,
    TenantId TenantId,
    string Email,
    string? FirstName,
    string? LastName,
    string? AvatarUrl,
    UserRole Role,
    DateTimeOffset? LastSeenAt,
    DateTimeOffset CreatedAt,
    string TenantName,
    SubscriptionPlan TenantPlan,
    int RolloutBucket,
    bool IsEnabled,
    FeatureFlagSource Source,
    int? InclusionThresholdPercentage,
    bool DefaultEnabled,
    DateTimeOffset? OverrideEnabledAt,
    DateTimeOffset? OverrideDisabledAt,
    AbInclusionPin? UserAbInclusionPin
);

public sealed class GetFeatureFlagUsersValidator : AbstractValidator<GetFeatureFlagUsersQuery>
{
    public GetFeatureFlagUsersValidator()
    {
        RuleFor(x => x.FlagKey)
            .NotEmpty().WithMessage("Feature flag key must not be empty.")
            .Must(key => SharedKernel.FeatureFlags.FeatureFlags.Get(key) is not null).WithMessage("Feature flag key must exist in the registry.")
            .Must(key => SharedKernel.FeatureFlags.FeatureFlags.Get(key)?.Scope == FeatureFlagScope.User).WithMessage("Feature flag must have user scope.");

        RuleFor(x => x.Search).MaximumLength(100).WithMessage("The search term must be at most 100 characters.");
        RuleFor(x => x.Roles.Length).LessThanOrEqualTo(10).WithMessage("Roles filter must contain no more than 10 values.");
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100).WithMessage("Page size must be between 1 and 100.");
        RuleFor(x => x.PageOffset).GreaterThanOrEqualTo(0).WithMessage("Page offset must be greater than or equal to 0.");
    }
}

public sealed class GetFeatureFlagUsersHandler(IFeatureFlagRepository featureFlagRepository, IUserRepository userRepository, ITenantRepository tenantRepository, TimeProvider timeProvider)
    : IRequestHandler<GetFeatureFlagUsersQuery, Result<GetFeatureFlagUsersResponse>>
{
    public async Task<Result<GetFeatureFlagUsersResponse>> Handle(GetFeatureFlagUsersQuery query, CancellationToken cancellationToken)
    {
        var definition = SharedKernel.FeatureFlags.FeatureFlags.Get(query.FlagKey);
        if (definition is null) return Result<GetFeatureFlagUsersResponse>.NotFound($"Feature flag with key '{query.FlagKey}' not found.");

        var (users, _, _) = await userRepository.SearchAllUsersUnfilteredAsync(
            query.Search ?? string.Empty,
            query.Roles,
            null,
            timeProvider.GetUtcNow(),
            SortableBackOfficeUserProperties.Name,
            SortOrder.Ascending,
            0,
            int.MaxValue,
            cancellationToken
        );

        var userOverrides = await featureFlagRepository.GetUserOverridesForFlagAsync(query.FlagKey, cancellationToken);
        var overridesByUserId = userOverrides.ToDictionary(f => f.UserId!);

        var baseRow = await featureFlagRepository.GetByKeyAndScopeAsync(query.FlagKey, null, null, cancellationToken);

        var tenantIds = users.Select(u => u.TenantId).Distinct().ToArray();
        var tenants = await tenantRepository.GetByIdsUnfilteredAsync(tenantIds, cancellationToken);
        var tenantsById = tenants.ToDictionary(t => t.Id);

        var featureFlagUsers = users.Select(user =>
            {
                tenantsById.TryGetValue(user.TenantId, out var tenant);
                var (isEnabled, source) = EvaluateOverride(definition, baseRow, overridesByUserId, user);
                overridesByUserId.TryGetValue(user.Id, out var userOverrideRow);

                return user.Adapt<FeatureFlagUserInfo>() with
                {
                    AvatarUrl = user.Avatar.Url,
                    TenantName = tenant?.Name ?? "Unknown",
                    TenantPlan = tenant?.Plan ?? SubscriptionPlan.Basis,
                    IsEnabled = isEnabled,
                    Source = source,
                    InclusionThresholdPercentage = ComputeInclusionThresholdPercentage(definition, user.RolloutBucket, user.AbInclusionPin, query.FlagKey),
                    DefaultEnabled = ComputeDefaultEnabled(definition, baseRow, user.RolloutBucket, user.AbInclusionPin),
                    OverrideEnabledAt = userOverrideRow?.EnabledAt,
                    OverrideDisabledAt = userOverrideRow?.DisabledAt,
                    UserAbInclusionPin = user.AbInclusionPin
                };
            }
        ).ToArray();

        // Aggregate stats reflect the population AFTER search/roles filtering but BEFORE state/has-override
        // filtering, so they describe "the addressable users for this flag" rather than the current view.
        var enabledCount = featureFlagUsers.Count(u => u.IsEnabled);
        var disabledCount = featureFlagUsers.Length - enabledCount;
        var overrideCount = featureFlagUsers.Count(u => u.Source == FeatureFlagSource.Manual);

        var filtered = query.State switch
        {
            FeatureFlagAudienceState.Enabled => featureFlagUsers.Where(u => u.IsEnabled).ToArray(),
            FeatureFlagAudienceState.Disabled => featureFlagUsers.Where(u => !u.IsEnabled).ToArray(),
            _ => featureFlagUsers
        };

        if (query.HasOverride == true)
        {
            filtered = filtered.Where(u => u.Source == FeatureFlagSource.Manual).ToArray();
        }

        var ordered = Sort(filtered, query.OrderBy, query.SortOrder).ToArray();

        var totalCount = ordered.Length;
        var totalPages = totalCount == 0 ? 0 : (totalCount - 1) / query.PageSize + 1;
        if (query.PageOffset > 0 && query.PageOffset >= totalPages)
        {
            return Result<GetFeatureFlagUsersResponse>.BadRequest($"The page offset '{query.PageOffset}' is greater than the total number of pages.");
        }

        var paged = ordered.Skip(query.PageOffset * query.PageSize).Take(query.PageSize).ToArray();

        return new GetFeatureFlagUsersResponse(totalCount, query.PageSize, totalPages, query.PageOffset, enabledCount, disabledCount, overrideCount, paged);
    }

    private static IEnumerable<FeatureFlagUserInfo> Sort(FeatureFlagUserInfo[] items, SortableFeatureFlagUserProperties orderBy, SortOrder sortOrder)
    {
        // Stable tie-break by Id keeps paginated results deterministic. Name is composed from FirstName + LastName + Email so callers see a single column.
        return (orderBy, sortOrder) switch
        {
            (SortableFeatureFlagUserProperties.TenantName, SortOrder.Ascending) => items.OrderBy(u => u.TenantName).ThenBy(u => u.Id.Value),
            (SortableFeatureFlagUserProperties.TenantName, _) => items.OrderByDescending(u => u.TenantName).ThenBy(u => u.Id.Value),
            (SortableFeatureFlagUserProperties.Role, SortOrder.Ascending) => items.OrderBy(u => u.Role).ThenBy(u => u.Id.Value),
            (SortableFeatureFlagUserProperties.Role, _) => items.OrderByDescending(u => u.Role).ThenBy(u => u.Id.Value),
            (SortableFeatureFlagUserProperties.LastSeenAt, SortOrder.Ascending) => items.OrderBy(u => u.LastSeenAt ?? DateTimeOffset.MaxValue).ThenBy(u => u.Id.Value),
            (SortableFeatureFlagUserProperties.LastSeenAt, _) => items.OrderByDescending(u => u.LastSeenAt ?? DateTimeOffset.MinValue).ThenBy(u => u.Id.Value),
            (SortableFeatureFlagUserProperties.IsEnabled, SortOrder.Ascending) => items.OrderBy(u => u.IsEnabled).ThenBy(u => u.Id.Value),
            (SortableFeatureFlagUserProperties.IsEnabled, _) => items.OrderByDescending(u => u.IsEnabled).ThenBy(u => u.Id.Value),
            (SortableFeatureFlagUserProperties.OverrideUpdatedAt, SortOrder.Ascending) => items.OrderBy(u => u.OverrideDisabledAt ?? u.OverrideEnabledAt ?? DateTimeOffset.MaxValue).ThenBy(u => u.Id.Value),
            (SortableFeatureFlagUserProperties.OverrideUpdatedAt, _) => items.OrderByDescending(u => u.OverrideDisabledAt ?? u.OverrideEnabledAt ?? DateTimeOffset.MinValue).ThenBy(u => u.Id.Value),
            (SortableFeatureFlagUserProperties.InclusionThresholdPercentage, SortOrder.Ascending) => items.OrderBy(u => u.InclusionThresholdPercentage ?? int.MaxValue).ThenBy(u => u.Id.Value),
            (SortableFeatureFlagUserProperties.InclusionThresholdPercentage, _) => items.OrderByDescending(u => u.InclusionThresholdPercentage ?? int.MinValue).ThenBy(u => u.Id.Value),
            (SortableFeatureFlagUserProperties.Name, SortOrder.Descending) => items.OrderByDescending(NameSortKey).ThenBy(u => u.Id.Value),
            _ => items.OrderBy(NameSortKey).ThenBy(u => u.Id.Value)
        };
    }

    private static string NameSortKey(FeatureFlagUserInfo user)
    {
        // Mirror the front-office convention: "FirstName LastName", falling back to email when both are null.
        return $"{user.FirstName} {user.LastName}".Trim() is { Length: > 0 } composed ? composed : user.Email;
    }

    // Pins are unconditional: AlwaysOn includes the user regardless of rollout, NeverOn excludes them
    // regardless of rollout. Mirrors FeatureFlagEvaluator's precedence chain.
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

    // Pins are unconditional and bypass the rollout, so AlwaysOn → 0 (always included) and
    // NeverOn → null (never included by rollout). Without a pin we fall back to the per-key threshold.
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
        Dictionary<UserId, FeatureFlag> overridesByUserId,
        User user
    )
    {
        if (overridesByUserId.TryGetValue(user.Id, out var userOverride))
        {
            // Gate on baseRow.IsActive so a globally-Deactivated kill-switch flag reports as disabled even when
            // an override row exists — matching the runtime FeatureFlagEvaluator.cs:48 short-circuit.
            var isOverrideActive = userOverride.EnabledAt is not null && (userOverride.DisabledAt is null || userOverride.EnabledAt > userOverride.DisabledAt);
            var isEnabled = baseRow is { IsActive: true } && isOverrideActive;
            return (isEnabled, FeatureFlagSource.Manual);
        }

        // Gate by baseRow.IsActive so a globally-deactivated flag never shows as Enabled in the
        // bulk admin view — matches FeatureFlagEvaluator.EvaluateAsync which skips entirely when
        // the base row is inactive.
        if (definition.IsAbTestEligible && baseRow is { IsActive: true })
        {
            return (EvaluateAbRollout(baseRow, user.RolloutBucket, user.AbInclusionPin), FeatureFlagSource.AbRollout);
        }

        return (false, FeatureFlagSource.Default);
    }
}
