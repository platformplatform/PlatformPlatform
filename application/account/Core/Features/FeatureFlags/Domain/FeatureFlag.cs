using JetBrains.Annotations;
using SharedKernel.Domain;
using SharedKernel.StronglyTypedIds;

namespace Account.Features.FeatureFlags.Domain;

public sealed class FeatureFlag : AggregateRoot<FeatureFlagId>
{
    [UsedImplicitly]
    private FeatureFlag() : base(FeatureFlagId.NewId())
    {
        FeatureFlagKey = string.Empty;
    }

    private FeatureFlag(string featureFlagKey, TenantId? tenantId, UserId? userId, FeatureFlagSource source)
        : base(FeatureFlagId.NewId())
    {
        FeatureFlagKey = featureFlagKey;
        TenantId = tenantId;
        UserId = userId;
        Source = source;
    }

    public string FeatureFlagKey { get; private set; }

    public TenantId? TenantId { get; private set; }

    public UserId? UserId { get; private set; }

    public DateTimeOffset? EnabledAt { get; private set; }

    public DateTimeOffset? DisabledAt { get; private set; }

    public int? RolloutBucketStart { get; private set; }

    public int? RolloutBucketEnd { get; private set; }

    [UsedImplicitly]
    public bool ConfigurableByTenant { get; private set; }

    [UsedImplicitly]
    public bool ConfigurableByUser { get; private set; }

    public FeatureFlagSource Source { get; private set; }

    public static FeatureFlag Create(string featureFlagKey, FeatureFlagSource source = FeatureFlagSource.Manual)
    {
        return new FeatureFlag(featureFlagKey, null, null, source);
    }

    public static FeatureFlag CreateTenantOverride(string featureFlagKey, TenantId tenantId, FeatureFlagSource source = FeatureFlagSource.Manual)
    {
        return new FeatureFlag(featureFlagKey, tenantId, null, source);
    }

    public static FeatureFlag CreateUserOverride(string featureFlagKey, TenantId tenantId, UserId userId)
    {
        return new FeatureFlag(featureFlagKey, tenantId, userId, FeatureFlagSource.Manual);
    }

    public void Activate(DateTimeOffset now)
    {
        EnabledAt = now;
        DisabledAt = null;
    }

    public void Deactivate(DateTimeOffset now)
    {
        if (EnabledAt is null) return;

        DisabledAt = now;
    }

    public void SetRolloutRange(int? rolloutBucketStart, int? rolloutBucketEnd)
    {
        if (rolloutBucketStart is null != rolloutBucketEnd is null)
        {
            throw new ArgumentException("Rollout bucket start and rollout bucket end must both be set or both be null.");
        }

        if (rolloutBucketStart is not null && (rolloutBucketStart < 0 || rolloutBucketStart > 99))
        {
            throw new ArgumentOutOfRangeException(nameof(rolloutBucketStart), "Rollout bucket start must be between 0 and 99.");
        }

        if (rolloutBucketEnd is not null && (rolloutBucketEnd < 0 || rolloutBucketEnd > 99))
        {
            throw new ArgumentOutOfRangeException(nameof(rolloutBucketEnd), "Rollout bucket end must be between 0 and 99.");
        }

        RolloutBucketStart = rolloutBucketStart;
        RolloutBucketEnd = rolloutBucketEnd;
    }
}

[PublicAPI]
[IdPrefix("fflag")]
[JsonConverter(typeof(StronglyTypedIdJsonConverter<string, FeatureFlagId>))]
public sealed record FeatureFlagId(string Value) : StronglyTypedUlid<FeatureFlagId>(Value)
{
    public override string ToString()
    {
        return Value;
    }
}
