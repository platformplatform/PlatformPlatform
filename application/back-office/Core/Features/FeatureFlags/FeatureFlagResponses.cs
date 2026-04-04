using JetBrains.Annotations;
using SharedKernel.Domain;
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
    DateTimeOffset? CreatedAt,
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
public sealed record FlagTenantInfo(TenantId TenantId, string TenantName, int RolloutBucket, bool IsEnabled, string Source);

[PublicAPI]
public sealed record GetFlagUsersResponse(FlagUserInfo[] Users);

[PublicAPI]
public sealed record FlagUserInfo(
    UserId UserId,
    string Email,
    string TenantName,
    int RolloutBucket,
    bool IsEnabled,
    string Source
);
