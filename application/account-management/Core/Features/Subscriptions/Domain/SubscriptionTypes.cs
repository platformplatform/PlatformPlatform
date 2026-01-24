using JetBrains.Annotations;

namespace PlatformPlatform.AccountManagement.Features.Subscriptions.Domain;

[PublicAPI]
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum SubscriptionPlan
{
    Trial,
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
