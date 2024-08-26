using Microsoft.AspNetCore.Http;
using PlatformPlatform.SharedKernel.ApplicationCore.Authentication;

namespace PlatformPlatform.AccountManagement.Application.Authentication;

public sealed class AuthenticationTokenService(AuthenticationTokenGenerator tokenGenerator, IHttpContextAccessor httpContextAccessor)
{
    public void CreateAndSetAuthenticationTokens(User user)
    {
        var httpContext = httpContextAccessor.HttpContext ?? throw new InvalidOperationException("HttpContext is null.");

        var refreshToken = tokenGenerator.GenerateRefreshToken(user.Id);
        httpContext.Response.Headers.Remove(AuthenticationTokenHttpKeys.RefreshTokenHttpHeaderKey);
        httpContext.Response.Headers.Append(AuthenticationTokenHttpKeys.RefreshTokenHttpHeaderKey, refreshToken);

        var accessToken = tokenGenerator.GenerateAccessToken(user);
        httpContext.Response.Headers.Remove(AuthenticationTokenHttpKeys.AccessTokenHttpHeaderKey);
        httpContext.Response.Headers.Append(AuthenticationTokenHttpKeys.AccessTokenHttpHeaderKey, accessToken);
    }

    public void RefreshAuthenticationTokens(User user, string refreshTokenChainId, int currentRefreshTokenVersion, DateTimeOffset expires)
    {
        var httpContext = httpContextAccessor.HttpContext ?? throw new InvalidOperationException("HttpContext is null.");

        var refreshToken = tokenGenerator.UpdateRefreshToken(user.Id, refreshTokenChainId, currentRefreshTokenVersion, expires);
        httpContext.Response.Headers.Remove(AuthenticationTokenHttpKeys.RefreshTokenHttpHeaderKey);
        httpContext.Response.Headers.Append(AuthenticationTokenHttpKeys.RefreshTokenHttpHeaderKey, refreshToken);

        var accessToken = tokenGenerator.GenerateAccessToken(user);
        httpContext.Response.Headers.Remove(AuthenticationTokenHttpKeys.AccessTokenHttpHeaderKey);
        httpContext.Response.Headers.Append(AuthenticationTokenHttpKeys.AccessTokenHttpHeaderKey, accessToken);
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
