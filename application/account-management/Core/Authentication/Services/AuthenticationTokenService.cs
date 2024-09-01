using Microsoft.AspNetCore.Http;
using PlatformPlatform.AccountManagement.Core.Users.Domain;
using PlatformPlatform.SharedKernel.Authentication;

namespace PlatformPlatform.AccountManagement.Core.Authentication.Services;

public sealed class AuthenticationTokenService(AuthenticationTokenGenerator tokenGenerator, IHttpContextAccessor httpContextAccessor)
{
    public void CreateAndSetAuthenticationTokens(User user)
    {
        var refreshToken = tokenGenerator.GenerateRefreshToken(user.Id);
        var accessToken = tokenGenerator.GenerateAccessToken(user);
        SetAuthenticationTokensOnHttpResponse(refreshToken, accessToken);
    }

    public void RefreshAuthenticationTokens(User user, string refreshTokenChainId, int currentRefreshTokenVersion, DateTimeOffset expires)
    {
        var refreshToken = tokenGenerator.UpdateRefreshToken(user.Id, refreshTokenChainId, currentRefreshTokenVersion, expires);
        var accessToken = tokenGenerator.GenerateAccessToken(user);
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
