using JetBrains.Annotations;
using SharedKernel.FeatureFlags;

namespace BackOffice.Features.FeatureFlags;

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
    DateTimeOffset? EnabledAt,
    DateTimeOffset? DisabledAt,
    int? BucketStart,
    int? BucketEnd,
    int? RolloutPercentage,
    bool IsActive
);

[PublicAPI]
public sealed record GetFlagTenantsResponse(FlagTenantInfo[] Tenants);

[PublicAPI]
public sealed record FlagTenantInfo(long TenantId, string TenantName, bool IsEnabled, string Source);
