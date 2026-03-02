using JetBrains.Annotations;

namespace Account.Features.Tenants.Domain;

[PublicAPI]
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum TenantState
{
    Active,
    Suspended
}

[PublicAPI]
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum SuspensionReason
{
    PaymentFailed,
    CustomerDeleted
}
