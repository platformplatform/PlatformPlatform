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
public sealed record GetFeatureFlagUsersResponse(int TotalCount, int PageSize, int TotalPages, int CurrentPageOffset, FeatureFlagUserInfo[] Users);

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
    bool DefaultEnabled
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

                return user.Adapt<FeatureFlagUserInfo>() with
                {
                    AvatarUrl = user.Avatar.Url,
                    TenantName = tenant?.Name ?? "Unknown",
                    TenantPlan = tenant?.Plan ?? SubscriptionPlan.Basis,
                    IsEnabled = isEnabled,
                    Source = source,
                    InclusionThresholdPercentage = definition.IsAbTestEligible
                        ? RolloutBucketHasher.ComputeInclusionThresholdPercentage(user.RolloutBucket, query.FlagKey)
                        : null,
                    DefaultEnabled = ComputeDefaultEnabled(definition, baseRow, user.RolloutBucket)
                };
            }
        ).ToArray();

        // featureFlagUsers is already name-ascending from SearchAllUsersUnfilteredAsync; subsequent filters preserve order.
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

        var totalCount = filtered.Length;
        var totalPages = totalCount == 0 ? 0 : (totalCount - 1) / query.PageSize + 1;
        if (query.PageOffset > 0 && query.PageOffset >= totalPages)
        {
            return Result<GetFeatureFlagUsersResponse>.BadRequest($"The page offset '{query.PageOffset}' is greater than the total number of pages.");
        }

        var paged = filtered.Skip(query.PageOffset * query.PageSize).Take(query.PageSize).ToArray();

        return new GetFeatureFlagUsersResponse(totalCount, query.PageSize, totalPages, query.PageOffset, paged);
    }

    // The state a user would have if no manual override existed: in-range for A/B-eligible flags
    // (and the base row is active), otherwise false (non-A/B flags require the user to opt in).
    private static bool ComputeDefaultEnabled(FeatureFlagDefinition definition, FeatureFlag? baseRow, int rolloutBucket)
    {
        if (baseRow is null || !baseRow.IsActive) return false;
        if (!definition.IsAbTestEligible) return false;
        if (baseRow.BucketStart is null || baseRow.BucketEnd is null) return false;
        return RolloutBucketHasher.IsInRolloutBucketRange(rolloutBucket, baseRow.BucketStart.Value, baseRow.BucketEnd.Value);
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
            var isEnabled = userOverride.EnabledAt is not null && (userOverride.DisabledAt is null || userOverride.EnabledAt > userOverride.DisabledAt);
            return (isEnabled, FeatureFlagSource.Manual);
        }

        if (definition.IsAbTestEligible && baseRow?.BucketStart is not null && baseRow.BucketEnd is not null)
        {
            var isInRange = RolloutBucketHasher.IsInRolloutBucketRange(user.RolloutBucket, baseRow.BucketStart.Value, baseRow.BucketEnd.Value);
            return (isInRange, FeatureFlagSource.AbRollout);
        }

        return (false, FeatureFlagSource.Default);
    }
}
