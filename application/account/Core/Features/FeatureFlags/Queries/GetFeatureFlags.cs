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
    int? BucketStart,
    int? BucketEnd,
    int? RolloutPercentage,
    bool IsActive
);

public sealed class GetFeatureFlagsHandler(IFeatureFlagRepository featureFlagRepository, IConfiguration configuration)
    : IRequestHandler<GetFeatureFlagsQuery, Result<GetFeatureFlagsResponse>>
{
    public async Task<Result<GetFeatureFlagsResponse>> Handle(GetFeatureFlagsQuery request, CancellationToken cancellationToken)
    {
        var definitions = SharedKernel.FeatureFlags.FeatureFlags.GetAll();
        var baseRows = await featureFlagRepository.GetAllBaseRowsAsync(cancellationToken);
        var baseRowsByKey = baseRows.ToDictionary(f => f.FlagKey);

        var flags = definitions.Select(definition =>
            {
                if (definition.Scope == FeatureFlagScope.System)
                {
                    var isSystemFlagActive = IsSystemFlagEnabled(definition.Key);
                    return new FeatureFlagInfo(
                        definition.Key, definition.Scope, definition.AdminLevel, definition.Description,
                        definition.IsAbTestEligible, definition.ConfigurableByTenant, definition.ConfigurableByUser, definition.RequiredPlan?.ToString(),
                        null, null, null, null, null, null, isSystemFlagActive
                    );
                }

                baseRowsByKey.TryGetValue(definition.Key, out var baseRow);

                var createdAt = baseRow?.CreatedAt;
                var enabledAt = baseRow?.EnabledAt;
                var disabledAt = baseRow?.DisabledAt;
                var bucketStart = baseRow?.BucketStart;
                var bucketEnd = baseRow?.BucketEnd;
                var isActive = enabledAt is not null && (disabledAt is null || enabledAt > disabledAt);
                var rolloutPercentage = ComputeRolloutPercentage(bucketStart, bucketEnd);

                return new FeatureFlagInfo(
                    definition.Key, definition.Scope, definition.AdminLevel, definition.Description,
                    definition.IsAbTestEligible, definition.ConfigurableByTenant, definition.ConfigurableByUser, definition.RequiredPlan?.ToString(),
                    createdAt, enabledAt, disabledAt, bucketStart, bucketEnd, rolloutPercentage, isActive
                );
            }
        ).ToArray();

        return new GetFeatureFlagsResponse(flags);
    }

    private bool IsSystemFlagEnabled(string flagKey)
    {
        return flagKey switch
        {
            "google-oauth" => !string.IsNullOrEmpty(configuration["OAuth:Google:ClientId"]),
            "subscriptions" => configuration["Stripe:SubscriptionEnabled"] == "true",
            _ => false
        };
    }

    private static int? ComputeRolloutPercentage(int? bucketStart, int? bucketEnd)
    {
        if (bucketStart is null || bucketEnd is null) return null;

        // 100% rollout uses reserved range 0-100
        if (bucketStart == 0 && bucketEnd == 100) return 100;

        if (bucketStart <= bucketEnd)
        {
            return bucketEnd.Value - bucketStart.Value + 1;
        }

        // Wrap-around case within 1-99 range
        return 99 - bucketStart.Value + 1 + bucketEnd.Value;
    }
}
