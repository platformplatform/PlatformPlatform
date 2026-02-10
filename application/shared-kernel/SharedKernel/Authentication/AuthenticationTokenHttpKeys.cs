namespace PlatformPlatform.SharedKernel.Authentication;

public static class AuthenticationTokenHttpKeys
{
    public const string RefreshTokenHttpHeaderKey = "x-refresh-token";

    public const string AccessTokenHttpHeaderKey = "x-access-token";

    public const string AntiforgeryTokenHttpHeaderKey = "x-xsrf-token";

    public const string RefreshAuthenticationTokensHeaderKey = "x-refresh-authentication-tokens-required";

    public const string UnauthorizedReasonHeaderKey = "x-unauthorized-reason";

    // __Host prefix ensures the cookie is sent only to the host, requires Secure, HTTPS, Path=/ and no Domain specified
    public const string RefreshTokenCookieName = "__Host-refresh-token";

    public const string AccessTokenCookieName = "__Host-access-token";

    public const string AntiforgeryTokenCookieName = "__Host-xsrf-token";
}
