using Account.Features.FeatureFlags.Domain;
using Account.Features.Subscriptions.Domain;
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
public sealed record GetFeatureFlagUsersQuery : IRequest<Result<GetFeatureFlagUsersResponse>>
{
    [JsonIgnore] // Removes from API contract
    public string FlagKey { get; init; } = null!;

    public string? Search { get; init; }
}

[PublicAPI]
public sealed record GetFeatureFlagUsersResponse(FeatureFlagUserInfo[] Users);

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
    string Source
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
    }
}

public sealed class GetFeatureFlagUsersHandler(IFeatureFlagRepository featureFlagRepository, IUserRepository userRepository, ITenantRepository tenantRepository)
    : IRequestHandler<GetFeatureFlagUsersQuery, Result<GetFeatureFlagUsersResponse>>
{
    public async Task<Result<GetFeatureFlagUsersResponse>> Handle(GetFeatureFlagUsersQuery query, CancellationToken cancellationToken)
    {
        var definition = SharedKernel.FeatureFlags.FeatureFlags.Get(query.FlagKey);
        if (definition is null) return Result<GetFeatureFlagUsersResponse>.NotFound($"Feature flag with key '{query.FlagKey}' not found.");

        if (string.IsNullOrWhiteSpace(query.Search)) return new GetFeatureFlagUsersResponse([]);

        var users = await userRepository.SearchByEmailUnfilteredAsync(query.Search.Trim(), cancellationToken);
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
                    Source = source
                };
            }
        ).ToArray();

        return new GetFeatureFlagUsersResponse(featureFlagUsers);
    }

    private static (bool IsEnabled, string Source) EvaluateOverride(
        FeatureFlagDefinition definition,
        FeatureFlag? baseRow,
        Dictionary<string, FeatureFlag> overridesByUserId,
        User user
    )
    {
        if (overridesByUserId.TryGetValue(user.Id.Value, out var userOverride))
        {
            var isEnabled = userOverride.EnabledAt is not null && (userOverride.DisabledAt is null || userOverride.EnabledAt > userOverride.DisabledAt);
            return (isEnabled, "manual_override");
        }

        if (definition.IsAbTestEligible && baseRow?.BucketStart is not null && baseRow.BucketEnd is not null)
        {
            var isInRange = RolloutBucketHasher.IsInRolloutBucketRange(user.RolloutBucket, baseRow.BucketStart.Value, baseRow.BucketEnd.Value);
            return (isInRange, "ab_rollout");
        }

        return (false, "default");
    }
}
