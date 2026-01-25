using JetBrains.Annotations;

namespace PlatformPlatform.Account.Features.Tenants.Domain;

[PublicAPI]
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum TenantState
{
    Trial,
    Active,
    PastDue,
    Suspended
}
