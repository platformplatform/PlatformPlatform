using Account.Features.FeatureFlags.Domain;
using JetBrains.Annotations;
using Microsoft.Extensions.Configuration;
using SharedKernel.Cqrs;
using SharedKernel.FeatureFlags;

namespace Account.Features.FeatureFlags.Queries;

[PublicAPI]
public sealed record GetFeatureFlagsQuery(bool IncludeDeleted = false) : IRequest<Result<GetFeatureFlagsResponse>>;

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
    bool IsStableModule,
    DateTimeOffset? OrphanedAt,
    DateTimeOffset? DeletedAt
);

public sealed class GetFeatureFlagsHandler(IFeatureFlagRepository featureFlagRepository, IConfiguration configuration)
    : IRequestHandler<GetFeatureFlagsQuery, Result<GetFeatureFlagsResponse>>
{
    public async Task<Result<GetFeatureFlagsResponse>> Handle(GetFeatureFlagsQuery request, CancellationToken cancellationToken)
    {
        var definitions = SharedKernel.FeatureFlags.FeatureFlags.GetAll();
        var baseRows = await featureFlagRepository.GetAllBaseRowsAsync(cancellationToken);
        var baseRowsByKey = baseRows.ToDictionary(f => f.FlagKey);

        var activeFlags = definitions.Select(definition =>
            {
                if (definition.Scope == FeatureFlagScope.System)
                {
                    var isSystemFeatureFlagActive = definition.IsSystemFeatureFlagEnabled(configuration);
                    return new FeatureFlagInfo(
                        definition.Key, definition.Scope, definition.AdminLevel, definition.Description,
                        definition.IsAbTestEligible, definition.ConfigurableByTenant, definition.ConfigurableByUser, definition.RequiredPlan?.ToString(),
                        null, null, null, null, null, null, isSystemFeatureFlagActive, definition.IsKillSwitchEnabled, definition.IsStableModule, null, null
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
                    createdAt, enabledAt, disabledAt, rolloutBucketStart, rolloutBucketEnd, rolloutPercentage, isActive, definition.IsKillSwitchEnabled, definition.IsStableModule, baseRow?.OrphanedAt, baseRow?.DeletedAt
                );
            }
        );

        // Surface orphaned (and optionally soft-deleted) rows whose key is no longer in the C# definitions
        // so admins can review and retire them. Scope is row-persisted; other definition-side fields use
        // safe defaults because no live behavior depends on them once the definition is gone.
        var definitionKeys = definitions.Select(d => d.Key).ToHashSet();
        var historicalFlags = baseRowsByKey.Values
            .Where(row => !definitionKeys.Contains(row.FlagKey))
            .Where(row => row.DeletedAt is null || request.IncludeDeleted)
            .Select(row => new FeatureFlagInfo(
                    row.FlagKey, row.Scope, FeatureFlagAdminLevel.SystemAdmin, string.Empty,
                    row.BucketStart is not null || row.BucketEnd is not null, false, false, null,
                    row.CreatedAt, row.EnabledAt, row.DisabledAt, row.BucketStart, row.BucketEnd,
                    ComputeRolloutPercentage(row.BucketStart, row.BucketEnd), row.IsActive, false, false, row.OrphanedAt, row.DeletedAt
                )
            );

        return new GetFeatureFlagsResponse(activeFlags.Concat(historicalFlags).ToArray());
    }

    private static int? ComputeRolloutPercentage(int? rolloutBucketStart, int? rolloutBucketEnd)
    {
        return RolloutBucketHasher.ComputeRolloutPercentage(rolloutBucketStart, rolloutBucketEnd);
    }
}
