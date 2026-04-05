using Account.Features.FeatureFlags.Domain;
using JetBrains.Annotations;
using Microsoft.Extensions.Configuration;
using SharedKernel.Cqrs;
using SharedKernel.Domain;

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
    SubscriptionPlan? RequiredPlan,
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
        var featureFlagDefinitions = SharedKernel.Domain.FeatureFlags.GetAll();
        var baseFeatureFlags = await featureFlagRepository.GetAllBaseFeatureFlagsAsync(cancellationToken);
        var baseFeatureFlagsByKey = baseFeatureFlags.ToDictionary(f => f.FeatureFlagKey);

        var featureFlags = featureFlagDefinitions.Select(featureFlagDefinition =>
            {
                if (featureFlagDefinition is SystemFeatureFlagDefinition systemFlag)
                {
                    var isSystemFeatureFlagActive = systemFlag.IsSystemFeatureFlagEnabled(configuration);
                    return new FeatureFlagInfo(
                        featureFlagDefinition.Key, featureFlagDefinition.Scope, featureFlagDefinition.AdminLevel, featureFlagDefinition.Description,
                        featureFlagDefinition.IsAbTestEligible, featureFlagDefinition.ConfigurableByTenant, featureFlagDefinition.ConfigurableByUser, featureFlagDefinition.RequiredPlan,
                        null, null, null, null, null, null, isSystemFeatureFlagActive
                    );
                }

                baseFeatureFlagsByKey.TryGetValue(new FeatureFlagKey(featureFlagDefinition.Key), out var baseFeatureFlag);

                var createdAt = baseFeatureFlag?.CreatedAt;
                var enabledAt = baseFeatureFlag?.EnabledAt;
                var disabledAt = baseFeatureFlag?.DisabledAt;
                var rolloutBucketStart = baseFeatureFlag?.RolloutBucketStart;
                var rolloutBucketEnd = baseFeatureFlag?.RolloutBucketEnd;
                var isActive = enabledAt is not null && (disabledAt is null || enabledAt > disabledAt);
                var rolloutPercentage = ComputeRolloutPercentage(rolloutBucketStart, rolloutBucketEnd);

                return new FeatureFlagInfo(
                    featureFlagDefinition.Key, featureFlagDefinition.Scope, featureFlagDefinition.AdminLevel, featureFlagDefinition.Description,
                    featureFlagDefinition.IsAbTestEligible, featureFlagDefinition.ConfigurableByTenant, featureFlagDefinition.ConfigurableByUser, featureFlagDefinition.RequiredPlan,
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
