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
    FeatureFlagSource Source,
    bool IsBaseRowActive,
    int RolloutBucket,
    TenantId TenantId,
    int? InclusionThresholdPercentage,
    bool DefaultEnabled,
    AbInclusionPin? UserAbInclusionPin
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

        var allRows = await featureFlagRepository.GetUserScopedRowsAsync(user.TenantId, user.Id, cancellationToken);
        var baseRowsByKey = allRows.Where(r => r.TenantId is null && r.UserId is null).ToDictionary(r => r.FlagKey);
        var userOverridesByKey = allRows.Where(r => r.UserId == user.Id).ToDictionary(r => r.FlagKey);

        var flags = userScopedDefinitions
            .Select(definition => Evaluate(definition, baseRowsByKey, userOverridesByKey, user.RolloutBucket, user.TenantId, user.AbInclusionPin))
            .ToArray();

        return new GetUserFeatureFlagsResponse(flags);
    }

    private static UserFeatureFlagInfo Evaluate(
        FeatureFlagDefinition definition,
        Dictionary<string, FeatureFlag> baseRowsByKey,
        Dictionary<string, FeatureFlag> userOverridesByKey,
        int userRolloutBucket,
        TenantId tenantId,
        AbInclusionPin? abInclusionPin
    )
    {
        baseRowsByKey.TryGetValue(definition.Key, out var baseRow);
        var isBaseRowActive = baseRow?.IsActive == true;
        userOverridesByKey.TryGetValue(definition.Key, out var userOverride);

        bool isEnabled;
        FeatureFlagSource source;

        if (userOverride is not null)
        {
            // Gate on baseRow.IsActive so a globally-Deactivated kill-switch flag reports as disabled even when
            // an override row exists — matching the runtime FeatureFlagEvaluator.cs:48 short-circuit.
            isEnabled = isBaseRowActive && userOverride.IsActive;
            source = FeatureFlagSource.Manual;
        }
        else if (definition.IsAbTestEligible)
        {
            isEnabled = isBaseRowActive && EvaluateAbRollout(baseRow, userRolloutBucket, abInclusionPin);
            source = FeatureFlagSource.AbRollout;
        }
        else
        {
            isEnabled = false;
            source = FeatureFlagSource.Default;
        }

        var defaultEnabled = ComputeDefaultEnabled(definition, baseRow, isBaseRowActive, userRolloutBucket, abInclusionPin);
        var inclusionThresholdPercentage = ComputeInclusionThresholdPercentage(definition, userRolloutBucket, abInclusionPin);

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
            tenantId,
            inclusionThresholdPercentage,
            defaultEnabled,
            abInclusionPin
        );
    }

    private static bool ComputeDefaultEnabled(FeatureFlagDefinition definition, FeatureFlag? baseRow, bool isBaseRowActive, int rolloutBucket, AbInclusionPin? abInclusionPin)
    {
        if (!isBaseRowActive) return false;
        if (!definition.IsAbTestEligible) return false;
        return EvaluateAbRollout(baseRow, rolloutBucket, abInclusionPin);
    }

    private static bool EvaluateAbRollout(FeatureFlag? baseRow, int rolloutBucket, AbInclusionPin? abInclusionPin)
    {
        if (abInclusionPin is AbInclusionPin.AlwaysOn) return true;
        if (abInclusionPin is AbInclusionPin.NeverOn) return false;
        if (baseRow?.BucketStart is null || baseRow.BucketEnd is null) return false;
        return RolloutBucketHasher.IsInRolloutBucketRange(rolloutBucket, baseRow.BucketStart.Value, baseRow.BucketEnd.Value);
    }

    // Pins are unconditional and bypass the rollout, so AlwaysOn → 0 (always included) and
    // NeverOn → null (never included by rollout). Without a pin we fall back to the per-key threshold.
    private static int? ComputeInclusionThresholdPercentage(FeatureFlagDefinition definition, int rolloutBucket, AbInclusionPin? abInclusionPin)
    {
        if (!definition.IsAbTestEligible) return null;
        if (abInclusionPin is AbInclusionPin.AlwaysOn) return 0;
        if (abInclusionPin is AbInclusionPin.NeverOn) return null;
        return RolloutBucketHasher.ComputeInclusionThresholdPercentage(rolloutBucket, definition.Key);
    }

    private static int? ComputeRolloutPercentage(int? bucketStart, int? bucketEnd)
    {
        return RolloutBucketHasher.ComputeRolloutPercentage(bucketStart, bucketEnd);
    }
}
