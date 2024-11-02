using Microsoft.AspNetCore.Http;
using PlatformPlatform.AccountManagement.Features.Users.Domain;
using PlatformPlatform.SharedKernel.Authentication;

namespace PlatformPlatform.AccountManagement.Features.Authentication.Services;

public sealed class AuthenticationTokenService(
    RefreshTokenGenerator refreshTokenGenerator,
    AccessTokenGenerator accessTokenGenerator,
    IHttpContextAccessor httpContextAccessor
)
{
    public void CreateAndSetAuthenticationTokens(User user)
    {
        var refreshToken = refreshTokenGenerator.Generate(user);
        var accessToken = accessTokenGenerator.Generate(user);
        SetAuthenticationTokensOnHttpResponse(refreshToken, accessToken);
    }

    public void RefreshAuthenticationTokens(User user, RefreshTokenId refreshTokenId, int currentRefreshTokenVersion, DateTimeOffset expires)
    {
        var refreshToken = refreshTokenGenerator.Update(user, refreshTokenId, currentRefreshTokenVersion, expires);
        var accessToken = accessTokenGenerator.Generate(user);
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
