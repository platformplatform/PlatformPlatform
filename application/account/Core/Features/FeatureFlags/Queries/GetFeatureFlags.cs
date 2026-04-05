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
    bool IsActive
);

public sealed class GetFeatureFlagsHandler(IFeatureFlagRepository featureFlagRepository, IConfiguration configuration)
    : IRequestHandler<GetFeatureFlagsQuery, Result<GetFeatureFlagsResponse>>
{
    public async Task<Result<GetFeatureFlagsResponse>> Handle(GetFeatureFlagsQuery request, CancellationToken cancellationToken)
    {
        var featureFlagDefinitions = SharedKernel.FeatureFlags.FeatureFlags.GetAll();
        var baseRows = await featureFlagRepository.GetAllBaseRowsAsync(cancellationToken);
        var baseRowsByKey = baseRows.ToDictionary(f => f.FeatureFlagKey);

        var featureFlags = featureFlagDefinitions.Select(featureFlagDefinition =>
            {
                if (featureFlagDefinition.Scope == FeatureFlagScope.System)
                {
                    var isSystemFeatureFlagActive = featureFlagDefinition.IsSystemFeatureFlagEnabled(configuration);
                    return new FeatureFlagInfo(
                        featureFlagDefinition.Key, featureFlagDefinition.Scope, featureFlagDefinition.AdminLevel, featureFlagDefinition.Description,
                        featureFlagDefinition.IsAbTestEligible, featureFlagDefinition.ConfigurableByTenant, featureFlagDefinition.ConfigurableByUser, featureFlagDefinition.RequiredPlan?.ToString(),
                        null, null, null, null, null, null, isSystemFeatureFlagActive
                    );
                }

                baseRowsByKey.TryGetValue(featureFlagDefinition.Key, out var baseRow);

                var createdAt = baseRow?.CreatedAt;
                var enabledAt = baseRow?.EnabledAt;
                var disabledAt = baseRow?.DisabledAt;
                var rolloutBucketStart = baseRow?.RolloutBucketStart;
                var rolloutBucketEnd = baseRow?.RolloutBucketEnd;
                var isActive = enabledAt is not null && (disabledAt is null || enabledAt > disabledAt);
                var rolloutPercentage = ComputeRolloutPercentage(rolloutBucketStart, rolloutBucketEnd);

                return new FeatureFlagInfo(
                    featureFlagDefinition.Key, featureFlagDefinition.Scope, featureFlagDefinition.AdminLevel, featureFlagDefinition.Description,
                    featureFlagDefinition.IsAbTestEligible, featureFlagDefinition.ConfigurableByTenant, featureFlagDefinition.ConfigurableByUser, featureFlagDefinition.RequiredPlan?.ToString(),
                    createdAt, enabledAt, disabledAt, rolloutBucketStart, rolloutBucketEnd, rolloutPercentage, isActive
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
