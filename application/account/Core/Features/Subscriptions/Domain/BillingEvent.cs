using System.Security.Cryptography;
using System.Text;
using Account.Features.Tenants.Domain;
using JetBrains.Annotations;
using SharedKernel.Domain;
using SharedKernel.StronglyTypedIds;

namespace Account.Features.Subscriptions.Domain;

/// <summary>
///     Strongly-typed ID for <see cref="BillingEvent" />. Unlike most aggregate IDs in the codebase
///     (which extend <c>StronglyTypedUlid</c> and generate fresh ULIDs at creation time), this ID is
///     issued as a deterministic SHA-256 hash of the event's identity components. Webhook redelivery
///     after a transaction rollback re-runs detection and produces the same ID, making the append
///     helper's existence-check skip path naturally idempotent.
/// </summary>
[PublicAPI]
[IdPrefix("bilev")]
[JsonConverter(typeof(StronglyTypedIdJsonConverter<string, BillingEventId>))]
public sealed record BillingEventId(string Value) : StronglyTypedString<BillingEventId>(Value)
{
    public override string ToString()
    {
        return Value;
    }

    /// <summary>
    ///     Builds a deterministic ID from the inputs that anchor a billing event to Stripe data.
    ///     Re-running reconciliation for the same Stripe state produces the same ID, so reconciliation
    ///     becomes a clean upsert with no duplicates and no fresh ULIDs on every sync.
    /// </summary>
    public static BillingEventId FromComponents(SubscriptionId subscriptionId, BillingEventType eventType, string stripeReference, DateTimeOffset occurredAt)
    {
        var key = $"{subscriptionId.Value}|{eventType}|{stripeReference}|{occurredAt:O}";
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(key));
        // Take 16 bytes (128 bits — the same width as a ULID) and base32-encode without padding.
        var token = Base32Encode(hash.AsSpan(0, 16));
        return NewId($"bilev_{token}");
    }

    private static string Base32Encode(ReadOnlySpan<byte> bytes)
    {
        // RFC 4648 base32 extended hex alphabet (NOT Crockford / ULID): 0-9 then A-V. We use this rather
        // than ULID's Crockford alphabet because the ID is a pure SHA-256 hash, never sorted by time,
        // never read by humans for typing, and never compared visually with ULIDs in the same UI.
        const string alphabet = "0123456789ABCDEFGHIJKLMNOPQRSTUV";
        Span<char> output = stackalloc char[26];
        var bitBuffer = 0;
        var bitCount = 0;
        var outputIndex = 0;
        foreach (var b in bytes)
        {
            bitBuffer = (bitBuffer << 8) | b;
            bitCount += 8;
            while (bitCount >= 5 && outputIndex < output.Length)
            {
                bitCount -= 5;
                output[outputIndex++] = alphabet[(bitBuffer >> bitCount) & 0x1F];
            }
        }

        if (outputIndex < output.Length && bitCount > 0)
        {
            output[outputIndex++] = alphabet[(bitBuffer << (5 - bitCount)) & 0x1F];
        }

        return new string(output[..outputIndex]);
    }
}

/// <summary>
///     A durable, append-only record of a subscription/billing lifecycle transition. Each row is the
///     authoritative log of what actually happened — once written, it is never updated and never
///     deleted. New rows are appended only when a real transition is detected during a Stripe sync.
///     Deterministic IDs make webhook retries idempotent: a redelivered event computes the same ID
///     and is silently skipped on PK conflict, so the log never accumulates duplicates.
///     A separate <c>BillingDriftDetector</c> service compares this log against Stripe history and
///     surfaces discrepancies in the back-office UI. It never rewrites history — manual reconciliation
///     is an explicit admin action, not an automatic side effect of the sync.
/// </summary>
public sealed class BillingEvent : AggregateRoot<BillingEventId>, ITenantScopedEntity
{
    private BillingEvent(BillingEventId id, TenantId tenantId) : base(id)
    {
        TenantId = tenantId;
        SubscriptionId = null!;
        EventType = default;
        OccurredAt = default;
        StripeReference = string.Empty;
    }

    public SubscriptionId SubscriptionId { get; private set; }

    public BillingEventType EventType { get; private set; }

    public SubscriptionPlan? FromPlan { get; private set; }

    public SubscriptionPlan? ToPlan { get; private set; }

    public decimal? PreviousAmount { get; private set; }

    public decimal? NewAmount { get; private set; }

    public decimal? AmountDelta { get; private set; }

    public string? Currency { get; private set; }

    public int? DaysOnPreviousPlan { get; private set; }

    public int? DaysUntilEffective { get; private set; }

    public int? DaysSinceCancelled { get; private set; }

    public DateTimeOffset? ScheduledFor { get; private set; }

    public DateTimeOffset? EffectiveAt { get; private set; }

    public DateTimeOffset OccurredAt { get; private set; }

    public CancellationReason? CancellationReason { get; private set; }

    public SuspensionReason? SuspensionReason { get; private set; }

    public string StripeReference { get; private set; }

    public TenantId TenantId { get; }

    public static BillingEvent Create(
        SubscriptionId subscriptionId,
        TenantId tenantId,
        BillingEventType eventType,
        DateTimeOffset occurredAt,
        string stripeReference,
        SubscriptionPlan? fromPlan = null,
        SubscriptionPlan? toPlan = null,
        decimal? previousAmount = null,
        decimal? newAmount = null,
        decimal? amountDelta = null,
        string? currency = null,
        int? daysOnPreviousPlan = null,
        int? daysUntilEffective = null,
        int? daysSinceCancelled = null,
        DateTimeOffset? scheduledFor = null,
        DateTimeOffset? effectiveAt = null,
        CancellationReason? cancellationReason = null,
        SuspensionReason? suspensionReason = null
    )
    {
        var id = BillingEventId.FromComponents(subscriptionId, eventType, stripeReference, occurredAt);
        return new BillingEvent(id, tenantId)
        {
            SubscriptionId = subscriptionId,
            EventType = eventType,
            OccurredAt = occurredAt,
            StripeReference = stripeReference,
            FromPlan = fromPlan,
            ToPlan = toPlan,
            PreviousAmount = previousAmount,
            NewAmount = newAmount,
            AmountDelta = amountDelta,
            Currency = currency,
            DaysOnPreviousPlan = daysOnPreviousPlan,
            DaysUntilEffective = daysUntilEffective,
            DaysSinceCancelled = daysSinceCancelled,
            ScheduledFor = scheduledFor,
            EffectiveAt = effectiveAt,
            CancellationReason = cancellationReason,
            SuspensionReason = suspensionReason
        };
    }
}

[PublicAPI]
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum BillingEventType
{
    SubscriptionCreated,
    SubscriptionRenewed,
    SubscriptionUpgraded,
    SubscriptionDowngradeScheduled,
    SubscriptionDowngradeCancelled,
    SubscriptionDowngraded,
    SubscriptionCancelled,
    SubscriptionReactivated,
    SubscriptionExpired,
    SubscriptionImmediatelyCancelled,
    SubscriptionSuspended,
    PaymentFailed,
    PaymentRecovered,
    PaymentRefunded,
    BillingInfoAdded,
    BillingInfoUpdated,
    PaymentMethodUpdated
}
