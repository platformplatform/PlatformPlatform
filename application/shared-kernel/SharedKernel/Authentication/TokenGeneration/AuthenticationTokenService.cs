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

    public void RefreshAuthenticationTokens(UserInfo userInfo, SessionId sessionId, RefreshTokenJti jti, int currentRefreshTokenVersion, DateTimeOffset expires)
    {
        var refreshToken = refreshTokenGenerator.Update(userInfo, sessionId, jti, currentRefreshTokenVersion, expires);
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
        httpContext.Response.Cookies.Delete(AuthenticationTokenHttpKeys.RefreshTokenCookieName);
        httpContext.Response.Cookies.Delete(AuthenticationTokenHttpKeys.AccessTokenCookieName);
    }
}
