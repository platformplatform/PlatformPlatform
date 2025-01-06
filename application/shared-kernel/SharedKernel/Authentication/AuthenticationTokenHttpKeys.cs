namespace PlatformPlatform.SharedKernel.Authentication;

public static class AuthenticationTokenHttpKeys
{
    public const string RefreshTokenHttpHeaderKey = "x-refresh-token";

    public const string AccessTokenHttpHeaderKey = "x-access-token";

    public const string AntiforgeryTokenHttpHeaderKey = "x-xsrf-token";

    public const string RefreshAuthenticationTokensHeaderKey = "x-refresh-authentication-tokens-required";

    // __Host prefix ensures the cookie is sent only to the host, requires Secure, HTTPS, Path=/ and no Domain specified
    public const string RefreshTokenCookieName = "__Host_Refresh_Token";

    public const string AccessTokenCookieName = "__Host_Access_Token";

    public const string AntiforgeryTokenCookieName = "__Host_Xsrf_Token";
}
