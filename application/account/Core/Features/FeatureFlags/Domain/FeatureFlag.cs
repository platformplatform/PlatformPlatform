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

    private FeatureFlag(string flagKey, long? tenantId, string? userId)
        : base(FeatureFlagId.NewId())
    {
        FlagKey = flagKey;
        TenantId = tenantId;
        UserId = userId;
    }

    public string FlagKey { get; private set; }

    public long? TenantId { get; private set; }

    public string? UserId { get; private set; }

    public DateTimeOffset? EnabledAt { get; private set; }

    public DateTimeOffset? DisabledAt { get; private set; }

    public int? BucketStart { get; private set; }

    public int? BucketEnd { get; private set; }

    [UsedImplicitly]
    public bool ConfigurableByTenant { get; private set; }

    [UsedImplicitly]
    public bool ConfigurableByUser { get; private set; }

    public static FeatureFlag Create(string flagKey)
    {
        return new FeatureFlag(flagKey, null, null);
    }

    public static FeatureFlag CreateTenantOverride(string flagKey, long tenantId)
    {
        return new FeatureFlag(flagKey, tenantId, null);
    }

    public static FeatureFlag CreateUserOverride(string flagKey, long tenantId, string userId)
    {
        return new FeatureFlag(flagKey, tenantId, userId);
    }

    public void Activate(DateTimeOffset now)
    {
        EnabledAt = now;
    }

    public void Deactivate(DateTimeOffset now)
    {
        DisabledAt = now;
    }

    public void SetRolloutRange(int? bucketStart, int? bucketEnd)
    {
        if (bucketStart is null != bucketEnd is null)
        {
            throw new ArgumentException("Bucket start and bucket end must both be set or both be null.");
        }

        if (bucketStart is not null && (bucketStart < 1 || bucketStart > 100))
        {
            throw new ArgumentOutOfRangeException(nameof(bucketStart), "Bucket start must be between 1 and 100.");
        }

        if (bucketEnd is not null && (bucketEnd < 1 || bucketEnd > 100))
        {
            throw new ArgumentOutOfRangeException(nameof(bucketEnd), "Bucket end must be between 1 and 100.");
        }

        BucketStart = bucketStart;
        BucketEnd = bucketEnd;
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
