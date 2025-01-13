namespace PlatformPlatform.SharedKernel.Authentication;

public static class AuthenticationTokenHttpKeys
{
    public const string RefreshTokenCookieName = "refresh_token";

    public const string AccessTokenCookieName = "access_token";

    public const string RefreshTokenHttpHeaderKey = "x-refresh-token";

    public const string AccessTokenHttpHeaderKey = "x-access-token";

    public const string RefreshAuthenticationTokensHeaderKey = "x-refresh-authentication-tokens-required";
}
