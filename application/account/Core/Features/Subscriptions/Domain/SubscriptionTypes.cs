using JetBrains.Annotations;

namespace PlatformPlatform.Account.Features.Subscriptions.Domain;

[PublicAPI]
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum SubscriptionPlan
{
    Basis,
    Standard,
    Premium
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
