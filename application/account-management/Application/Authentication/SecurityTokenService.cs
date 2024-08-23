using Microsoft.AspNetCore.Http;
using PlatformPlatform.SharedKernel.ApplicationCore.Authentication;

namespace PlatformPlatform.AccountManagement.Application.Authentication;

public sealed class SecurityTokenService(SecurityTokenGenerator tokenGenerator, IHttpContextAccessor httpContextAccessor)
{
    public void CreateAndSetSecurityTokens(User user)
    {
        var httpContext = httpContextAccessor.HttpContext ?? throw new InvalidOperationException("HttpContext is null.");

        var refreshToken = tokenGenerator.GenerateRefreshToken(user.Id);
        httpContext.Response.Headers.Remove(SecurityTokenSettings.RefreshTokenHttpHeaderKey);
        httpContext.Response.Headers.Append(SecurityTokenSettings.RefreshTokenHttpHeaderKey, refreshToken);

        var accessToken = tokenGenerator.GenerateAccessToken(user);
        httpContext.Response.Headers.Remove(SecurityTokenSettings.AccessTokenHttpHeaderKey);
        httpContext.Response.Headers.Append(SecurityTokenSettings.AccessTokenHttpHeaderKey, accessToken);
    }

    public void RefreshSecurityTokens(User user, string refreshTokenChainId, int currentRefreshTokenVersion, DateTimeOffset expires)
    {
        var httpContext = httpContextAccessor.HttpContext ?? throw new InvalidOperationException("HttpContext is null.");

        var refreshToken = tokenGenerator.UpdateRefreshToken(user.Id, refreshTokenChainId, currentRefreshTokenVersion, expires);
        httpContext.Response.Headers.Remove(SecurityTokenSettings.RefreshTokenHttpHeaderKey);
        httpContext.Response.Headers.Append(SecurityTokenSettings.RefreshTokenHttpHeaderKey, refreshToken);

        var accessToken = tokenGenerator.GenerateAccessToken(user);
        httpContext.Response.Headers.Remove(SecurityTokenSettings.AccessTokenHttpHeaderKey);
        httpContext.Response.Headers.Append(SecurityTokenSettings.AccessTokenHttpHeaderKey, accessToken);
    }

    public void Logout()
    {
        var httpContext = httpContextAccessor.HttpContext ?? throw new InvalidOperationException("HttpContext is null.");

        httpContext.Response.Headers.Remove(SecurityTokenSettings.RefreshTokenHttpHeaderKey);
        httpContext.Response.Headers.Remove(SecurityTokenSettings.AccessTokenHttpHeaderKey);
        httpContext.Response.Cookies.Delete(SecurityTokenSettings.AuthenticationCookieName);
    }
}
