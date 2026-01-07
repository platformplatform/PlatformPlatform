using JetBrains.Annotations;

namespace PlatformPlatform.AccountManagement.Features.Authentication.Domain;

[PublicAPI]
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum DeviceType
{
    Unknown,
    Desktop,
    Mobile,
    Tablet
}

[PublicAPI]
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum SessionRevokedReason
{
    LoggedOut,
    Revoked,
    ReplayAttackDetected,
    SwitchTenant
}
