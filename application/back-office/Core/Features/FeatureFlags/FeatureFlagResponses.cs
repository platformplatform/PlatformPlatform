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
    string? RequiredPlan,
    DateTimeOffset? CreatedAt,
    DateTimeOffset? EnabledAt,
    DateTimeOffset? DisabledAt,
    int? RolloutBucketStart,
    int? RolloutBucketEnd,
    int? RolloutPercentage,
    bool IsActive
);

[PublicAPI]
public sealed record GetFeatureFlagTenantsResponse(FeatureFlagTenantInfo[] Tenants);

[PublicAPI]
public sealed record FeatureFlagTenantInfo(
    TenantId TenantId,
    string TenantName,
    string Plan,
    int RolloutBucket,
    bool IsEnabled,
    string Source
);

[PublicAPI]
public sealed record GetFeatureFlagUsersResponse(FeatureFlagUserInfo[] Users);

[PublicAPI]
public sealed record FeatureFlagUserInfo(
    UserId UserId,
    TenantId TenantId,
    string Email,
    string TenantName,
    int RolloutBucket,
    bool IsEnabled,
    string Source
);
