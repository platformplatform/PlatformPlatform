using Account.Features.FeatureFlags.Domain;
using JetBrains.Annotations;
using Microsoft.Extensions.Configuration;
using SharedKernel.Cqrs;
using SharedKernel.FeatureFlags;

namespace Account.Features.FeatureFlags.Queries;

[PublicAPI]
public sealed record GetFeatureFlagsQuery : IRequest<Result<GetFeatureFlagsResponse>>;

[PublicAPI]
public sealed record GetFeatureFlagsResponse(FeatureFlagInfo[] Flags);

[PublicAPI]
public sealed record FeatureFlagInfo(
    string Key,
    FeatureFlagScope Scope,
    FeatureFlagAdminLevel AdminLevel,
    string Description,
    bool IsAbTestEligible,
    bool ConfigurableByTenant,
    bool ConfigurableByUser,
    string? RequiredPlan,
    DateTimeOffset? CreatedAt,
    DateTimeOffset? EnabledAt,
    DateTimeOffset? DisabledAt,
    int? RolloutBucketStart,
    int? RolloutBucketEnd,
    int? RolloutPercentage,
    bool IsActive,
    bool IsKillSwitchEnabled,
    DateTimeOffset? OrphanedAt
);

public sealed class GetFeatureFlagsHandler(IFeatureFlagRepository featureFlagRepository, IConfiguration configuration)
    : IRequestHandler<GetFeatureFlagsQuery, Result<GetFeatureFlagsResponse>>
{
    public async Task<Result<GetFeatureFlagsResponse>> Handle(GetFeatureFlagsQuery request, CancellationToken cancellationToken)
    {
        var definitions = SharedKernel.FeatureFlags.FeatureFlags.GetAll();
        var baseRows = await featureFlagRepository.GetAllBaseRowsAsync(cancellationToken);
        var baseRowsByKey = baseRows.ToDictionary(f => f.FlagKey);

        var featureFlags = definitions.Select(definition =>
            {
                if (definition.Scope == FeatureFlagScope.System)
                {
                    var isSystemFeatureFlagActive = definition.IsSystemFeatureFlagEnabled(configuration);
                    return new FeatureFlagInfo(
                        definition.Key, definition.Scope, definition.AdminLevel, definition.Description,
                        definition.IsAbTestEligible, definition.ConfigurableByTenant, definition.ConfigurableByUser, definition.RequiredPlan?.ToString(),
                        null, null, null, null, null, null, isSystemFeatureFlagActive, definition.IsKillSwitchEnabled, null
                    );
                }

                baseRowsByKey.TryGetValue(definition.Key, out var baseRow);

                var createdAt = baseRow?.CreatedAt;
                var enabledAt = baseRow?.EnabledAt;
                var disabledAt = baseRow?.DisabledAt;
                var rolloutBucketStart = baseRow?.BucketStart;
                var rolloutBucketEnd = baseRow?.BucketEnd;
                var isActive = enabledAt is not null && (disabledAt is null || enabledAt > disabledAt);
                var rolloutPercentage = ComputeRolloutPercentage(rolloutBucketStart, rolloutBucketEnd);

                return new FeatureFlagInfo(
                    definition.Key, definition.Scope, definition.AdminLevel, definition.Description,
                    definition.IsAbTestEligible, definition.ConfigurableByTenant, definition.ConfigurableByUser, definition.RequiredPlan?.ToString(),
                    createdAt, enabledAt, disabledAt, rolloutBucketStart, rolloutBucketEnd, rolloutPercentage, isActive, definition.IsKillSwitchEnabled, baseRow?.OrphanedAt
                );
            }
        ).ToArray();

        return new GetFeatureFlagsResponse(featureFlags);
    }

    private static int? ComputeRolloutPercentage(int? rolloutBucketStart, int? rolloutBucketEnd)
    {
        if (rolloutBucketStart is null || rolloutBucketEnd is null) return null;

        // 100% rollout uses reserved range 0-100
        if (rolloutBucketStart == 0 && rolloutBucketEnd == 100) return 100;

        if (rolloutBucketStart <= rolloutBucketEnd)
        {
            return rolloutBucketEnd.Value - rolloutBucketStart.Value + 1;
        }

        // Wrap-around case within 1-99 range
        return 99 - rolloutBucketStart.Value + 1 + rolloutBucketEnd.Value;
    }
}
