using Account.Features.FeatureFlags.Domain;
using Account.Features.Users.Domain;
using JetBrains.Annotations;
using SharedKernel.Cqrs;
using SharedKernel.Domain;
using SharedKernel.FeatureFlags;

namespace Account.Features.FeatureFlags.Queries;

[PublicAPI]
public sealed record GetUserFeatureFlagsQuery : IRequest<Result<GetUserFeatureFlagsResponse>>
{
    [JsonIgnore] // Removes from API contract
    public UserId UserId { get; init; } = null!;
}

[PublicAPI]
public sealed record GetUserFeatureFlagsResponse(UserFeatureFlagInfo[] Flags);

[PublicAPI]
public sealed record UserFeatureFlagInfo(
    string FlagKey,
    FeatureFlagScope Scope,
    string Description,
    bool IsAbTestEligible,
    int? BucketStart,
    int? BucketEnd,
    int? RolloutPercentage,
    bool IsEnabled,
    string Source,
    bool IsBaseRowActive,
    int RolloutBucket,
    TenantId TenantId
);

public sealed class GetUserFeatureFlagsHandler(IFeatureFlagRepository featureFlagRepository, IUserRepository userRepository)
    : IRequestHandler<GetUserFeatureFlagsQuery, Result<GetUserFeatureFlagsResponse>>
{
    public async Task<Result<GetUserFeatureFlagsResponse>> Handle(GetUserFeatureFlagsQuery query, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdUnfilteredAsync(query.UserId, cancellationToken);
        if (user is null) return Result<GetUserFeatureFlagsResponse>.NotFound($"User with ID '{query.UserId}' not found.");

        var userScopedDefinitions = SharedKernel.FeatureFlags.FeatureFlags.GetAll()
            .Where(f => f.Scope == FeatureFlagScope.User)
            .ToArray();

        var allRows = await featureFlagRepository.GetUserScopedRowsAsync(user.TenantId.Value, user.Id.Value, cancellationToken);
        var baseRowsByKey = allRows.Where(r => r.TenantId is null && r.UserId is null).ToDictionary(r => r.FlagKey);
        var userOverridesByKey = allRows.Where(r => r.UserId == user.Id.Value).ToDictionary(r => r.FlagKey);

        var flags = userScopedDefinitions
            .Select(definition => Evaluate(definition, baseRowsByKey, userOverridesByKey, user.RolloutBucket, user.TenantId))
            .ToArray();

        return new GetUserFeatureFlagsResponse(flags);
    }

    private static UserFeatureFlagInfo Evaluate(
        FeatureFlagDefinition definition,
        Dictionary<string, FeatureFlag> baseRowsByKey,
        Dictionary<string, FeatureFlag> userOverridesByKey,
        int userRolloutBucket,
        TenantId tenantId
    )
    {
        baseRowsByKey.TryGetValue(definition.Key, out var baseRow);
        var isBaseRowActive = baseRow is not null && IsActive(baseRow);
        userOverridesByKey.TryGetValue(definition.Key, out var userOverride);

        bool isEnabled;
        string source;

        if (userOverride is not null)
        {
            isEnabled = IsActive(userOverride);
            source = "manual_override";
        }
        else if (definition.IsAbTestEligible && baseRow?.BucketStart is not null && baseRow.BucketEnd is not null)
        {
            isEnabled = isBaseRowActive
                        && RolloutBucketHasher.IsInRolloutBucketRange(userRolloutBucket, baseRow.BucketStart.Value, baseRow.BucketEnd.Value);
            source = "ab_rollout";
        }
        else
        {
            isEnabled = false;
            source = "default";
        }

        return new UserFeatureFlagInfo(
            definition.Key,
            definition.Scope,
            definition.Description,
            definition.IsAbTestEligible,
            baseRow?.BucketStart,
            baseRow?.BucketEnd,
            ComputeRolloutPercentage(baseRow?.BucketStart, baseRow?.BucketEnd),
            isEnabled,
            source,
            isBaseRowActive,
            userRolloutBucket,
            tenantId
        );
    }

    private static bool IsActive(FeatureFlag featureFlag)
    {
        return featureFlag.EnabledAt is not null && (featureFlag.DisabledAt is null || featureFlag.EnabledAt > featureFlag.DisabledAt);
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
