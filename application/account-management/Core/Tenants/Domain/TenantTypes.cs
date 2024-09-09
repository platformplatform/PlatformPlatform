using JetBrains.Annotations;

namespace PlatformPlatform.AccountManagement.Tenants.Domain;

[PublicAPI]
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum TenantState
{
    Trial,
    Active,
    Suspended
}
