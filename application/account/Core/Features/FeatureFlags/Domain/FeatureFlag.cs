using System.ComponentModel.DataAnnotations.Schema;
using JetBrains.Annotations;
using SharedKernel.Domain;
using SharedKernel.StronglyTypedIds;

namespace Account.Features.FeatureFlags.Domain;

public sealed class FeatureFlag : AggregateRoot<FeatureFlagId>
{
    [UsedImplicitly]
    private FeatureFlag() : base(FeatureFlagId.NewId())
    {
        FlagKey = string.Empty;
    }

    private FeatureFlag(string flagKey, TenantId? tenantId, UserId? userId, FeatureFlagSource source)
        : base(FeatureFlagId.NewId())
    {
        FlagKey = flagKey;
        TenantId = tenantId;
        UserId = userId;
        Source = source;
    }

    public string FlagKey { get; private set; }

    public TenantId? TenantId { get; private set; }

    public UserId? UserId { get; private set; }

    public DateTimeOffset? EnabledAt { get; private set; }

    public DateTimeOffset? DisabledAt { get; private set; }

    public int? BucketStart { get; private set; }

    public int? BucketEnd { get; private set; }

    [UsedImplicitly]
    public bool ConfigurableByTenant { get; private set; }

    [UsedImplicitly]
    public bool ConfigurableByUser { get; private set; }

    public FeatureFlagSource Source { get; private set; }

    public DateTimeOffset? OrphanedAt { get; private set; }

    [NotMapped]
    public bool IsActive => EnabledAt is not null && (DisabledAt is null || EnabledAt > DisabledAt);

    public static FeatureFlag Create(string flagKey, FeatureFlagSource source = FeatureFlagSource.Manual)
    {
        return new FeatureFlag(flagKey, null, null, source);
    }

    public static FeatureFlag CreateTenantOverride(string flagKey, TenantId tenantId, FeatureFlagSource source = FeatureFlagSource.Manual)
    {
        return new FeatureFlag(flagKey, tenantId, null, source);
    }

    public static FeatureFlag CreateUserOverride(string flagKey, TenantId tenantId, UserId userId)
    {
        return new FeatureFlag(flagKey, tenantId, userId, FeatureFlagSource.Manual);
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

    public void SetSource(FeatureFlagSource source)
    {
        Source = source;
    }

    public void MarkOrphaned(DateTimeOffset now)
    {
        if (OrphanedAt is not null) return;

        OrphanedAt = now;
    }

    public void SetRolloutRange(int? rolloutBucketStart, int? rolloutBucketEnd)
    {
        if (rolloutBucketStart is null != rolloutBucketEnd is null)
        {
            throw new ArgumentException("Bucket start and bucket end must both be set or both be null.");
        }

        if (rolloutBucketStart is not null && (rolloutBucketStart < 0 || rolloutBucketStart > 99))
        {
            throw new ArgumentOutOfRangeException(nameof(rolloutBucketStart), "Bucket start must be between 0 and 99.");
        }

        if (rolloutBucketEnd is not null && (rolloutBucketEnd < 0 || rolloutBucketEnd > 99))
        {
            throw new ArgumentOutOfRangeException(nameof(rolloutBucketEnd), "Bucket end must be between 0 and 99.");
        }

        BucketStart = rolloutBucketStart;
        BucketEnd = rolloutBucketEnd;
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
