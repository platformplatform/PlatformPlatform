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
public enum LoginMethod
{
    OneTimePassword,
    Google
}

/// <summary>
///     Represents why a session was revoked. This is a domain concept stored in the Session aggregate.
///     For HTTP header reasons (which include additional cases like SessionNotFound), see
///     <see cref="SharedKernel.Authentication.UnauthorizedReason" />.
/// </summary>
[PublicAPI]
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum SessionRevokedReason
{
    LoggedOut,
    Revoked,
    ReplayAttackDetected,
    SwitchTenant
}
