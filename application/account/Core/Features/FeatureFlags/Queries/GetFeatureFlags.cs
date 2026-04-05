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
    SubscriptionPlan? RequiredSubscriptionPlan,
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
                        featureFlagDefinition.IsAbTestEligible, featureFlagDefinition.ConfigurableByTenant, featureFlagDefinition.ConfigurableByUser, featureFlagDefinition.RequiredSubscriptionPlan,
                        null, null, null, null, null, null, isSystemFeatureFlagActive
                    );
                }

                baseFeatureFlagsByKey.TryGetValue(featureFlagDefinition.Key, out var baseFeatureFlag);

                var createdAt = baseFeatureFlag?.CreatedAt;
                var enabledAt = baseFeatureFlag?.EnabledAt;
                var disabledAt = baseFeatureFlag?.DisabledAt;
                var rolloutBucketStart = baseFeatureFlag?.RolloutBucketStart;
                var rolloutBucketEnd = baseFeatureFlag?.RolloutBucketEnd;
                var isActive = enabledAt is not null && (disabledAt is null || enabledAt > disabledAt);
                var rolloutPercentage = ComputeRolloutPercentage(rolloutBucketStart, rolloutBucketEnd, featureFlagDefinition.IsAbTestEligible);

                return new FeatureFlagInfo(
                    featureFlagDefinition.Key, featureFlagDefinition.Scope, featureFlagDefinition.AdminLevel, featureFlagDefinition.Description,
                    featureFlagDefinition.IsAbTestEligible, featureFlagDefinition.ConfigurableByTenant, featureFlagDefinition.ConfigurableByUser, featureFlagDefinition.RequiredSubscriptionPlan,
                    createdAt, enabledAt, disabledAt, rolloutBucketStart, rolloutBucketEnd, rolloutPercentage, isActive
                );
            }
        ).ToArray();

        return new GetFeatureFlagsResponse(featureFlags);
    }

    private static int? ComputeRolloutPercentage(int? rolloutBucketStart, int? rolloutBucketEnd, bool isAbTestEligible)
    {
        if (rolloutBucketStart is null || rolloutBucketEnd is null) return isAbTestEligible ? 0 : null;

        // 100% rollout covers the full 0-99 range
        if (rolloutBucketStart == 0 && rolloutBucketEnd == 99) return 100;

        if (rolloutBucketStart <= rolloutBucketEnd)
        {
            return rolloutBucketEnd.Value - rolloutBucketStart.Value + 1;
        }

        // Wrap-around case within 1-99 range
        return 99 - rolloutBucketStart.Value + 1 + rolloutBucketEnd.Value;
    }
}
