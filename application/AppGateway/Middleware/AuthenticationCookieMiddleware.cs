using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.IdentityModel.Tokens;
using PlatformPlatform.SharedKernel.ApplicationCore.Authentication;

namespace PlatformPlatform.AppGateway.Middleware;

public class AuthenticationCookieMiddleware(
    IDataProtectionProvider dataProtectionProvider,
    SecurityTokenSettings securityTokenSettings,
    ILogger<AuthenticationCookieMiddleware> logger
)
    : IMiddleware
{
    private const string AuthenticationCookieName = "authentication-cookie";

    private readonly IDataProtector _protector = dataProtectionProvider.CreateProtector("PlatformPlatform");

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        if (context.Request.Cookies.TryGetValue(AuthenticationCookieName, out var authenticationCookieValue))
        {
            ConvertAuthenticationCookieToHttpBearerHeader(context, authenticationCookieValue);
        }

        await next(context);

        if (context.Response.Headers.TryGetValue(RefreshToken.XRefreshTokenKey, out var refreshToken) &&
            context.Response.Headers.TryGetValue(RefreshToken.XAccessTokenKey, out var accessToken))
        {
            ReplaceAuthenticationHeaderWithCookie(context, refreshToken.Single()!, accessToken.Single()!);
        }
    }

    private void ConvertAuthenticationCookieToHttpBearerHeader(HttpContext context, string authenticationCookieValue)
    {
        if (context.Request.Headers.ContainsKey(RefreshToken.XRefreshTokenKey) || context.Request.Headers.ContainsKey(RefreshToken.XAccessTokenKey))
        {
            // The authentication cookie is used by WebApp, but API requests should use tokens in the headers
            throw new InvalidOperationException("A request cannot contain both a session cookie and tokens in the headers.");
        }

        try
        {
            var authenticationTokenPair = Decrypt(authenticationCookieValue);

            // TODO: Check if AccessToken is expired, call the refresh token endpoint to get a new one here
            // We will do this by calling a token refresh endpoint that will issue a new access token, but also an
            // updated refresh token, with a new version number (but same ID), that can be used to detect replay attacks.

            context.Request.Headers.Append("Authorization", $"Bearer {authenticationTokenPair.AccessToken}");
        }
        catch (Exception ex)
        {
            context.Response.Cookies.Delete(AuthenticationCookieName);

            logger.LogWarning(ex, "Failed to decrypt authentication cookie. Removing cookie.");
        }
    }

    private void ReplaceAuthenticationHeaderWithCookie(HttpContext context, string refreshToken, string accessToken)
    {
        var refreshTokenExpires = ExtractExpirationFromToken(ValidateAndExtractClaims(refreshToken));

        var authenticationTokenPair = new AuthenticationTokenPair(refreshToken, accessToken);

        var encryptedToken = Encrypt(authenticationTokenPair);

        // The authentication cookie is SameSiteMode.Lax, unlike SameSiteMode.Strict this makes the cookie available
        // on the first request, which mean we can redirect to the login page if the user is not authenticated without
        // having to first serve the SPA. This is only secure if iFrames are not allowed to host the site.
        var cookieOptions = new CookieOptions
        {
            HttpOnly = true, Secure = true, SameSite = SameSiteMode.Lax, Expires = refreshTokenExpires
        };
        context.Response.Cookies.Append(AuthenticationCookieName, encryptedToken, cookieOptions);

        context.Response.Headers.Remove(RefreshToken.XRefreshTokenKey);
        context.Response.Headers.Remove(RefreshToken.XAccessTokenKey);
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

    public ClaimsPrincipal ValidateAndExtractClaims(string token)
    {
        var tokenHandler = new JwtSecurityTokenHandler();

        // Ensure the token is valid and can be read
        if (!tokenHandler.CanReadToken(token))
        {
            throw new SecurityTokenMalformedException("The token is not a valid JWT.");
        }

        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = securityTokenSettings.Issuer,

            ValidateAudience = true,
            ValidAudience = securityTokenSettings.Audience,

            ValidateLifetime = true,

            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(securityTokenSettings.GetKeyBytes()),

            ClockSkew = TimeSpan.Zero // No clock skew
        };

        SecurityToken validatedToken;

        var validateAndExtractClaims = tokenHandler.ValidateToken(token, validationParameters, out validatedToken);

        return validateAndExtractClaims;
    }

    public DateTime ExtractExpirationFromToken(ClaimsPrincipal principal)
    {
        // The 'exp' claim is the number of seconds since Unix epoch (00:00:00 UTC on 1st January 1970)
        var expClaim = principal.Claims.FirstOrDefault(claim => claim.Type == JwtRegisteredClaimNames.Exp)
                       ?? throw new InvalidOperationException("Expiration claim is missing from the token.");

        // Convert the expiration time from seconds since Unix epoch to DateTime
        var unixSeconds = long.Parse(expClaim.Value);

        return DateTimeOffset.FromUnixTimeSeconds(unixSeconds).UtcDateTime;
    }

    private sealed record AuthenticationTokenPair(string RefreshToken, string AccessToken);
}
