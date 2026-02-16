using JetBrains.Annotations;

namespace PlatformPlatform.Account.Features.Tenants.Domain;

[PublicAPI]
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum TenantState
{
    Active,
    PastDue,
    Suspended
}
