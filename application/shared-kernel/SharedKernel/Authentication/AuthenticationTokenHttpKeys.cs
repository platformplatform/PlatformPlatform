namespace PlatformPlatform.SharedKernel.Authentication;

public static class AuthenticationTokenHttpKeys
{
    public const string RefreshTokenCookieName = "refresh-token";

    public const string AccessTokenCookieName = "access-token";

    public const string RefreshTokenHttpHeaderKey = "X-Refresh-Token";

    public const string AccessTokenHttpHeaderKey = "X-Access-Token";
}
