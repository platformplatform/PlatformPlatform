using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.IdentityModel.Tokens;
using PlatformPlatform.SharedKernel.ApiCore;
using PlatformPlatform.SharedKernel.ApplicationCore.Authentication;

namespace PlatformPlatform.AppGateway.Middleware;

public class AuthenticationCookieMiddleware(
    IDataProtectionProvider dataProtectionProvider,
    SecurityTokenSettings securityTokenSettings,
    ILogger<AuthenticationCookieMiddleware> logger
)
    : IMiddleware
{
    private readonly IDataProtector _protector = dataProtectionProvider.CreateProtector("PlatformPlatform");

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        if (context.Request.Cookies.TryGetValue(SecurityTokenSettings.AuthenticationCookieName, out var authenticationCookieValue))
        {
            ValidateAuthenticationCookieAndConvertToHttpBearerHeader(context, authenticationCookieValue);
        }

        await next(context);

        if (context.Response.Headers.TryGetValue(SecurityTokenSettings.RefreshTokenHttpHeaderKey, out var refreshToken) &&
            context.Response.Headers.TryGetValue(SecurityTokenSettings.AccessTokenHttpHeaderKey, out var accessToken))
        {
            ReplaceAuthenticationHeaderWithCookie(context, refreshToken.Single()!, accessToken.Single()!);
        }
    }

    private void ValidateAuthenticationCookieAndConvertToHttpBearerHeader(HttpContext context, string authenticationCookieValue)
    {
        if (context.Request.Headers.ContainsKey(SecurityTokenSettings.RefreshTokenHttpHeaderKey) ||
            context.Request.Headers.ContainsKey(SecurityTokenSettings.AccessTokenHttpHeaderKey))
        {
            // The authentication cookie is used by WebApp, but API requests should use tokens in the headers
            throw new InvalidOperationException("A request cannot contain both a authentication cookie and security tokens in the headers.");
        }

        var authenticationTokenPair = Decrypt(authenticationCookieValue);
        try
        {
            if (ExtractExpirationFromToken(authenticationTokenPair.RefreshToken) < TimeProvider.System.GetUtcNow())
            {
                context.Response.Cookies.Delete(SecurityTokenSettings.AuthenticationCookieName);
                logger.LogWarning("The refresh-token has expired. The authentication cookie is removed.");
                return;
            }

            if (ExtractExpirationFromToken(authenticationTokenPair.AccessToken) < TimeProvider.System.GetUtcNow())
            {
                logger.LogDebug("The access-token has expired, and needs to be refreshed");
                // TODO: Use the refresh-token to geta a new Access Token endpoint.
                // Update the refresh-token with a new version number (but same ID) to prevent and detect replay attacks.
            }

            GetValidatedTokenClaims(authenticationTokenPair.AccessToken, true);
            context.Request.Headers.Append("Authorization", $"Bearer {authenticationTokenPair.AccessToken}");
        }
        catch (SecurityTokenException ex)
        {
            context.Response.Cookies.Delete(SecurityTokenSettings.AuthenticationCookieName);
            logger.LogWarning(ex, "The access-token could not be validated. The authentication cookie is removed. {Message}", ex.Message);
        }
    }

    private void ReplaceAuthenticationHeaderWithCookie(HttpContext context, string refreshToken, string accessToken)
    {
        var refreshTokenExpires = ExtractExpirationFromToken(refreshToken);

        var authenticationTokenPair = new AuthenticationTokenPair(refreshToken, accessToken);

        var encryptedToken = Encrypt(authenticationTokenPair);

        // The authentication cookie is SameSiteMode.Lax, unlike SameSiteMode.Strict this makes the cookie available
        // on the first request, which mean we can redirect to the login page if the user is not authenticated without
        // having to first serve the SPA. This is only secure if iFrames are not allowed to host the site.
        var cookieOptions = new CookieOptions
        {
            HttpOnly = true, Secure = true, SameSite = SameSiteMode.Lax, Expires = refreshTokenExpires
        };
        context.Response.Cookies.Append(SecurityTokenSettings.AuthenticationCookieName, encryptedToken, cookieOptions);

        context.Response.Headers.Remove(SecurityTokenSettings.RefreshTokenHttpHeaderKey);
        context.Response.Headers.Remove(SecurityTokenSettings.AccessTokenHttpHeaderKey);
    }

    private AuthenticationTokenPair Decrypt(string authenticationCookieValue)
    {
        var decryptedValue = _protector.Unprotect(authenticationCookieValue);
        return JsonSerializer.Deserialize<AuthenticationTokenPair>(decryptedValue)!;
    }

    private string Encrypt(AuthenticationTokenPair authenticationTokenPair)
    {
        var jsonString = JsonSerializer.Serialize(authenticationTokenPair);
        return _protector.Protect(jsonString);
    }

    private DateTimeOffset ExtractExpirationFromToken(string refreshToken)
    {
        var tokenClaims = GetValidatedTokenClaims(refreshToken, false);

        // The 'exp' claim is the number of seconds since Unix epoch (00:00:00 UTC on 1st January 1970)
        var expClaim = tokenClaims.Claims.FirstOrDefault(claim => claim.Type == JwtRegisteredClaimNames.Exp)
                       ?? throw new InvalidOperationException("Expiration claim is missing from the token.");

        // Convert the expiration time from seconds since Unix epoch to DateTime
        var unixSeconds = long.Parse(expClaim.Value);

        return DateTimeOffset.FromUnixTimeSeconds(unixSeconds);
    }

    private ClaimsPrincipal GetValidatedTokenClaims(string token, bool throwIfExpired)
    {
        var tokenHandler = new JwtSecurityTokenHandler();

        if (!tokenHandler.CanReadToken(token))
        {
            throw new SecurityTokenMalformedException("The token is not a valid JWT.");
        }

        var validationParameters = ApiCoreConfiguration.GetTokenValidationParameters(
            securityTokenSettings.Issuer,
            securityTokenSettings.Audience,
            securityTokenSettings.GetKeyBytes(),
            validateLifetime: throwIfExpired,
            clockSkew: TimeSpan.FromSeconds(2) // In Azure we don't need clock skew, but this must be a lower value than in downstream APIs
        );

        // This will throw if the token is invalid
        return tokenHandler.ValidateToken(token, validationParameters, out _);
    }

    private sealed record AuthenticationTokenPair(string RefreshToken, string AccessToken);
}
