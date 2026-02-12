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
    }

    public string EventType { get; private set; }

    public DateTimeOffset ProcessedAt { get; private set; }

    public string? StripeCustomerId { get; private set; }

    public string? StripeSubscriptionId { get; private set; }

    public long? TenantId { get; private set; }

    public string? Payload { get; private set; }

    public static StripeEvent Create(string stripeEventId, string eventType, DateTimeOffset processedAt, string? stripeCustomerId, string? stripeSubscriptionId, long? tenantId, string? payload)
    {
        return new StripeEvent(StripeEventId.NewId(stripeEventId))
        {
            EventType = eventType,
            ProcessedAt = processedAt,
            StripeCustomerId = stripeCustomerId,
            StripeSubscriptionId = stripeSubscriptionId,
            TenantId = tenantId,
            Payload = payload
        };
    }
}
