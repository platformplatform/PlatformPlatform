using JetBrains.Annotations;
using SharedKernel.Domain;

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
    SubscriptionPlan? RequiredPlan,
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
    SubscriptionPlan Plan,
    int RolloutBucket,
    bool IsEnabled,
    FeatureFlagOverrideSource Source
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
    FeatureFlagOverrideSource Source
);
