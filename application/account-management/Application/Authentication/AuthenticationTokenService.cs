using Microsoft.AspNetCore.Http;
using PlatformPlatform.SharedKernel.ApplicationCore.Authentication;

namespace PlatformPlatform.AccountManagement.Application.Authentication;

public sealed class AuthenticationTokenService(AuthenticationTokenGenerator tokenGenerator, IHttpContextAccessor httpContextAccessor)
{
    public void CreateAndSetAuthenticationTokens(User user)
    {
        var httpContext = httpContextAccessor.HttpContext ?? throw new InvalidOperationException("HttpContext is null.");

        var refreshToken = tokenGenerator.GenerateRefreshToken(user.Id);
        httpContext.Response.Headers.Remove(AuthenticationTokenSettings.RefreshTokenHttpHeaderKey);
        httpContext.Response.Headers.Append(AuthenticationTokenSettings.RefreshTokenHttpHeaderKey, refreshToken);

        var accessToken = tokenGenerator.GenerateAccessToken(user);
        httpContext.Response.Headers.Remove(AuthenticationTokenSettings.AccessTokenHttpHeaderKey);
        httpContext.Response.Headers.Append(AuthenticationTokenSettings.AccessTokenHttpHeaderKey, accessToken);
    }

    public void RefreshAuthenticationTokens(User user, string refreshTokenChainId, int currentRefreshTokenVersion, DateTimeOffset expires)
    {
        var httpContext = httpContextAccessor.HttpContext ?? throw new InvalidOperationException("HttpContext is null.");

        var refreshToken = tokenGenerator.UpdateRefreshToken(user.Id, refreshTokenChainId, currentRefreshTokenVersion, expires);
        httpContext.Response.Headers.Remove(AuthenticationTokenSettings.RefreshTokenHttpHeaderKey);
        httpContext.Response.Headers.Append(AuthenticationTokenSettings.RefreshTokenHttpHeaderKey, refreshToken);

        var accessToken = tokenGenerator.GenerateAccessToken(user);
        httpContext.Response.Headers.Remove(AuthenticationTokenSettings.AccessTokenHttpHeaderKey);
        httpContext.Response.Headers.Append(AuthenticationTokenSettings.AccessTokenHttpHeaderKey, accessToken);
    }

    public void Logout()
    {
        var httpContext = httpContextAccessor.HttpContext ?? throw new InvalidOperationException("HttpContext is null.");

        httpContext.Response.Headers.Remove(AuthenticationTokenSettings.RefreshTokenHttpHeaderKey);
        httpContext.Response.Headers.Remove(AuthenticationTokenSettings.AccessTokenHttpHeaderKey);
        httpContext.Response.Cookies.Delete(AuthenticationTokenSettings.RefreshTokenCookieName);
        httpContext.Response.Cookies.Delete(AuthenticationTokenSettings.AccessTokenCookieName);
    }
}
