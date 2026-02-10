using Microsoft.AspNetCore.Http;

namespace PlatformPlatform.SharedKernel.Authentication.TokenGeneration;

public sealed class AuthenticationTokenService(
    RefreshTokenGenerator refreshTokenGenerator,
    AccessTokenGenerator accessTokenGenerator,
    IHttpContextAccessor httpContextAccessor
)
{
    public void CreateAndSetAuthenticationTokens(UserInfo userInfo, SessionId sessionId, RefreshTokenJti jti)
    {
        var refreshToken = refreshTokenGenerator.Generate(userInfo, sessionId, jti);
        var accessToken = accessTokenGenerator.Generate(userInfo);
        SetAuthenticationTokensOnHttpResponse(refreshToken, accessToken);
    }

    /// <summary>Preserves the original expiry to prevent session lifetime extension through repeated tenant switching.</summary>
    public void SwitchTenantAndSetAuthenticationTokens(UserInfo userInfo, SessionId sessionId, RefreshTokenJti jti, DateTimeOffset expires)
    {
        var refreshToken = refreshTokenGenerator.Generate(userInfo, sessionId, jti, 1, expires);
        var accessToken = accessTokenGenerator.Generate(userInfo);
        SetAuthenticationTokensOnHttpResponse(refreshToken, accessToken);
    }

    /// <summary>Used during token refresh to issue new tokens with incremented version while preserving original expiry.</summary>
    public void GenerateAuthenticationTokens(UserInfo userInfo, SessionId sessionId, RefreshTokenJti jti, int refreshTokenVersion, DateTimeOffset expires)
    {
        var refreshToken = refreshTokenGenerator.Generate(userInfo, sessionId, jti, refreshTokenVersion, expires);
        var accessToken = accessTokenGenerator.Generate(userInfo);
        SetAuthenticationTokensOnHttpResponse(refreshToken, accessToken);
    }

    private void SetAuthenticationTokensOnHttpResponse(string refreshToken, string accessToken)
    {
        var httpContext = httpContextAccessor.HttpContext ?? throw new InvalidOperationException("HttpContext is null.");
        httpContext.Response.Headers[AuthenticationTokenHttpKeys.RefreshTokenHttpHeaderKey] = refreshToken;
        httpContext.Response.Headers[AuthenticationTokenHttpKeys.AccessTokenHttpHeaderKey] = accessToken;
    }

    public void Logout()
    {
        var httpContext = httpContextAccessor.HttpContext ?? throw new InvalidOperationException("HttpContext is null.");

        httpContext.Response.Headers.Remove(AuthenticationTokenHttpKeys.RefreshTokenHttpHeaderKey);
        httpContext.Response.Headers.Remove(AuthenticationTokenHttpKeys.AccessTokenHttpHeaderKey);
        var hostCookieOptions = new CookieOptions { Secure = true };
        httpContext.Response.Cookies.Delete(AuthenticationTokenHttpKeys.RefreshTokenCookieName, hostCookieOptions);
        httpContext.Response.Cookies.Delete(AuthenticationTokenHttpKeys.AccessTokenCookieName, hostCookieOptions);
    }
}
