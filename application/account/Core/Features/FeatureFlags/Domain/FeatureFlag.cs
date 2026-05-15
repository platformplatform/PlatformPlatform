using System.ComponentModel.DataAnnotations.Schema;
using JetBrains.Annotations;
using SharedKernel.Domain;
using SharedKernel.FeatureFlags;
using SharedKernel.StronglyTypedIds;

namespace Account.Features.FeatureFlags.Domain;

public sealed class FeatureFlag : AggregateRoot<FeatureFlagId>
{
    [UsedImplicitly]
    private FeatureFlag() : base(FeatureFlagId.NewId())
    {
        FlagKey = string.Empty;
    }

    private FeatureFlag(string flagKey, TenantId? tenantId, UserId? userId, FeatureFlagSource source, FeatureFlagScope scope)
        : base(FeatureFlagId.NewId())
    {
        FlagKey = flagKey;
        TenantId = tenantId;
        UserId = userId;
        Source = source;
        Scope = scope;
    }

    public DateTimeOffset? DeletedAt { get; private set; }

    public DateTimeOffset? OrphanedAt { get; private set; }

    public string FlagKey { get; private set; }

    public TenantId? TenantId { get; }

    public UserId? UserId { get; }

    public DateTimeOffset? EnabledAt { get; private set; }

    public DateTimeOffset? DisabledAt { get; private set; }

    public int? BucketStart { get; private set; }

    public int? BucketEnd { get; private set; }

    public FeatureFlagSource Source { get; private set; }

    // Persisted on every row so orphaned and soft-deleted base rows can still be grouped correctly in
    // the BackOffice (Tenant/User) after their definition is removed from FeatureFlags.cs. Override rows
    // also carry it for consistency with the base row they inherit from.
    public FeatureFlagScope Scope { get; private set; }

    [NotMapped]
    public bool IsActive => EnabledAt is not null && (DisabledAt is null || EnabledAt > DisabledAt);

    public static FeatureFlag Create(string flagKey, FeatureFlagScope scope, FeatureFlagSource source = FeatureFlagSource.Manual)
    {
        return new FeatureFlag(flagKey, null, null, source, scope);
    }

    public static FeatureFlag CreateTenantOverride(string flagKey, TenantId tenantId, FeatureFlagScope scope, FeatureFlagSource source = FeatureFlagSource.Manual)
    {
        return new FeatureFlag(flagKey, tenantId, null, source, scope);
    }

    public static FeatureFlag CreateUserOverride(string flagKey, TenantId tenantId, UserId userId, FeatureFlagScope scope, FeatureFlagSource source = FeatureFlagSource.Manual)
    {
        return new FeatureFlag(flagKey, tenantId, userId, source, scope);
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

    public void SetScope(FeatureFlagScope scope)
    {
        Scope = scope;
    }

    public void MarkOrphaned(DateTimeOffset now)
    {
        if (OrphanedAt is not null) return;

        OrphanedAt = now;
    }

    // Soft-delete is the only delete path: the base row is retained for historical telemetry so admin
    // surfaces can still resolve the flag key after the definition is removed from code. Tenant and user
    // override rows are hard-deleted by the command handler (they carry no historical value).
    public void MarkDeleted(DateTimeOffset now)
    {
        if (TenantId is not null || UserId is not null)
        {
            throw new InvalidOperationException("Only base feature flag rows can be soft-deleted.");
        }

        if (DeletedAt is not null) return;

        DeletedAt = now;
    }

    // Clears OrphanedAt and DeletedAt together. Called by the reconciler when a previously-removed flag
    // is re-added to FeatureFlags.cs (rollback or intentional reintroduction). Reuse is by design — the
    // existing base row keeps its history and telemetry continuity rather than starting a fresh row.
    public void Restore()
    {
        OrphanedAt = null;
        DeletedAt = null;
    }

    public void SetRolloutRange(int? rolloutBucketStart, int? rolloutBucketEnd)
    {
        if (rolloutBucketStart is null != rolloutBucketEnd is null)
        {
            throw new ArgumentException("Bucket start and bucket end must both be set or both be null.");
        }

        if (rolloutBucketStart is not null && (rolloutBucketStart < 0 || rolloutBucketStart > RolloutBucketHasher.MaxBucketInclusive))
        {
            throw new ArgumentOutOfRangeException(nameof(rolloutBucketStart), $"Bucket start must be between 0 and {RolloutBucketHasher.MaxBucketInclusive}.");
        }

        if (rolloutBucketEnd is not null && (rolloutBucketEnd < 0 || rolloutBucketEnd > RolloutBucketHasher.MaxBucketInclusive))
        {
            throw new ArgumentOutOfRangeException(nameof(rolloutBucketEnd), $"Bucket end must be between 0 and {RolloutBucketHasher.MaxBucketInclusive}.");
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
