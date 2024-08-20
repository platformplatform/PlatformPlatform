using Microsoft.AspNetCore.Http;
using PlatformPlatform.SharedKernel.ApplicationCore.Authentication;

namespace PlatformPlatform.AccountManagement.Application.Authentication;

public sealed class SecurityTokenService(SecurityTokenGenerator tokenGenerator, IHttpContextAccessor httpContextAccessor)
{
    public void CreateAndSetSecurityTokens(User user)
    {
        var httpContext = httpContextAccessor.HttpContext ?? throw new InvalidOperationException("HttpContext is null.");

        var refreshToken = tokenGenerator.GenerateRefreshToken(user);
        httpContext.Response.Headers.Remove(SecurityTokenSettings.RefreshTokenHttpHeaderKey);
        httpContext.Response.Headers.Append(SecurityTokenSettings.RefreshTokenHttpHeaderKey, refreshToken);

        var accessToken = tokenGenerator.GenerateAccessToken(user);
        httpContext.Response.Headers.Remove(SecurityTokenSettings.AccessTokenHttpHeaderKey);
        httpContext.Response.Headers.Append(SecurityTokenSettings.AccessTokenHttpHeaderKey, accessToken);
    }
}
