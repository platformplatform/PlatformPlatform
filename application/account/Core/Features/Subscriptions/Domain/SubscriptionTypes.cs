using JetBrains.Annotations;
using PlatformPlatform.SharedKernel.StronglyTypedIds;

namespace PlatformPlatform.Account.Features.Subscriptions.Domain;

[PublicAPI]
[IdPrefix("cus")]
[JsonConverter(typeof(StronglyTypedIdJsonConverter<string, StripeCustomerId>))]
public sealed record StripeCustomerId(string Value) : StronglyTypedString<StripeCustomerId>(Value)
{
    public override string ToString()
    {
        return Value;
    }
}

[PublicAPI]
[IdPrefix("sub")]
[JsonConverter(typeof(StronglyTypedIdJsonConverter<string, StripeSubscriptionId>))]
public sealed record StripeSubscriptionId(string Value) : StronglyTypedString<StripeSubscriptionId>(Value)
{
    public override string ToString()
    {
        return Value;
    }
}

[PublicAPI]
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum SubscriptionPlan
{
    Basis = 0,
    Standard = 1,
    Premium = 2
}

[PublicAPI]
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CancellationReason
{
    FoundAlternative,
    TooExpensive,
    NoLongerNeeded,
    Other,
    CancelledByAdmin
}

[PublicAPI]
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum PaymentTransactionStatus
{
    Succeeded,
    Failed,
    Pending,
    Refunded
}

[PublicAPI]
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum StripeEventStatus
{
    Pending,
    Processed,
    Ignored,
    Failed
}
