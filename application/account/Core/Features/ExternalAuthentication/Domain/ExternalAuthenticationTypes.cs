using JetBrains.Annotations;

namespace PlatformPlatform.Account.Features.ExternalAuthentication.Domain;

[PublicAPI]
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ExternalProviderType
{
    Google
}

[PublicAPI]
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ExternalFlowType
{
    Login,
    Signup
}

[PublicAPI]
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ExternalLoginResult
{
    Success,
    IdentityProviderError,
    InvalidState,
    LoginReplayDetected,
    SessionNotFound,
    FlowIdMismatch,
    SessionHijackingDetected,
    LoginExpired,
    LoginAlreadyCompleted,
    CodeExchangeFailed,
    IdentityMismatch,
    UserNotFound
}
