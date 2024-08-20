using System.Text.Json;
using Microsoft.AspNetCore.Http;
using PlatformPlatform.SharedKernel.ApplicationCore.Authentication;

namespace PlatformPlatform.AccountManagement.Application.Authentication;

public sealed class SecurityTokenService(SecurityTokenGenerator tokenGenerator, IHttpContextAccessor httpContextAccessor)
{
    public void CreateAndSetSecurityTokens(User user)
    {
        var httpContext = httpContextAccessor.HttpContext ?? throw new InvalidOperationException("HttpContext is null.");

        var refreshToken = tokenGenerator.GenerateRefreshToken(user);
        httpContext.Response.Headers.Remove(RefreshToken.XRefreshTokenKey);
        httpContext.Response.Headers.Append(RefreshToken.XRefreshTokenKey, JsonSerializer.Serialize(refreshToken));

        var accessToken = tokenGenerator.GenerateAccessToken(user);
        httpContext.Response.Headers.Remove(RefreshToken.XAccessTokenKey);
        httpContext.Response.Headers.Append(RefreshToken.XAccessTokenKey, accessToken);
    }
}
