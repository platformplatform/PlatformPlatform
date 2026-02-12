using JetBrains.Annotations;
using PlatformPlatform.SharedKernel.Domain;
using PlatformPlatform.SharedKernel.StronglyTypedIds;

namespace PlatformPlatform.Account.Features.Subscriptions.Domain;

[PublicAPI]
[IdPrefix("swev")]
[JsonConverter(typeof(StronglyTypedIdJsonConverter<string, StripeWebhookEventId>))]
public sealed record StripeWebhookEventId(string Value) : StronglyTypedUlid<StripeWebhookEventId>(Value)
{
    public override string ToString()
    {
        return Value;
    }
}

public sealed class StripeWebhookEvent : AggregateRoot<StripeWebhookEventId>
{
    private StripeWebhookEvent() : base(StripeWebhookEventId.NewId())
    {
        StripeEventId = string.Empty;
        EventType = string.Empty;
    }

    public string StripeEventId { get; private set; }

    public string EventType { get; private set; }

    public DateTimeOffset ProcessedAt { get; private set; }

    public string? StripeCustomerId { get; private set; }

    public string? StripeSubscriptionId { get; private set; }

    public long? TenantId { get; private set; }

    public string? Payload { get; private set; }

    public static StripeWebhookEvent Create(string stripeEventId, string eventType, DateTimeOffset processedAt, string? stripeCustomerId, string? stripeSubscriptionId, long? tenantId, string? payload)
    {
        return new StripeWebhookEvent
        {
            StripeEventId = stripeEventId,
            EventType = eventType,
            ProcessedAt = processedAt,
            StripeCustomerId = stripeCustomerId,
            StripeSubscriptionId = stripeSubscriptionId,
            TenantId = tenantId,
            Payload = payload
        };
    }
}
