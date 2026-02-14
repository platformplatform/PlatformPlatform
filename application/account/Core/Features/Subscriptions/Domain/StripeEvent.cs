using JetBrains.Annotations;
using PlatformPlatform.SharedKernel.Domain;
using PlatformPlatform.SharedKernel.StronglyTypedIds;

namespace PlatformPlatform.Account.Features.Subscriptions.Domain;

[PublicAPI]
[IdPrefix("evt")]
[JsonConverter(typeof(StronglyTypedIdJsonConverter<string, StripeEventId>))]
public sealed record StripeEventId(string Value) : StronglyTypedString<StripeEventId>(Value)
{
    public override string ToString()
    {
        return Value;
    }
}

public sealed class StripeEvent : AggregateRoot<StripeEventId>
{
    private StripeEvent(StripeEventId id) : base(id)
    {
        EventType = string.Empty;
        Status = StripeEventStatus.Pending;
    }

    public string EventType { get; private set; }

    public StripeEventStatus Status { get; private set; }

    public DateTimeOffset? ProcessedAt { get; private set; }

    public StripeCustomerId? StripeCustomerId { get; private set; }

    public StripeSubscriptionId? StripeSubscriptionId { get; private set; }

    public TenantId? TenantId { get; private set; }

    public string? Payload { get; private set; }

    public string? Error { get; private set; }

    /// <summary>
    ///     Factory method for phase 1 webhook acknowledgment. Creates a Pending event that will be
    ///     batch-processed in phase 2. TenantId and StripeSubscriptionId are backfilled by phase 2
    ///     via SetTenantId() and SetStripeSubscriptionId().
    /// </summary>
    public static StripeEvent Create(string stripeEventId, string eventType, StripeCustomerId? stripeCustomerId, string? payload)
    {
        return new StripeEvent(StripeEventId.NewId(stripeEventId))
        {
            EventType = eventType,
            StripeCustomerId = stripeCustomerId,
            Payload = payload
        };
    }

    /// <summary>
    ///     Marks the event as successfully processed during phase 2 batch processing.
    /// </summary>
    public void MarkProcessed(DateTimeOffset processedAt)
    {
        Status = StripeEventStatus.Processed;
        ProcessedAt = processedAt;
    }

    /// <summary>
    ///     Marks the event as ignored during phase 1 when no customer ID is present.
    /// </summary>
    public void MarkIgnored(DateTimeOffset processedAt)
    {
        Status = StripeEventStatus.Ignored;
        ProcessedAt = processedAt;
    }

    /// <summary>
    ///     Marks the event as failed with an error message when phase 2 processing encounters an error.
    /// </summary>
    public void MarkFailed(DateTimeOffset failedAt, string error)
    {
        Status = StripeEventStatus.Failed;
        ProcessedAt = failedAt;
        Error = error;
    }

    public void SetStripeSubscriptionId(StripeSubscriptionId? stripeSubscriptionId)
    {
        StripeSubscriptionId = stripeSubscriptionId;
    }

    public void SetTenantId(TenantId? tenantId)
    {
        TenantId = tenantId;
    }
}
