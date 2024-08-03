using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using PlatformPlatform.SharedKernel.ApplicationCore.Authentication;

namespace PlatformPlatform.AppGateway.Middleware;

public class AuthenticationCookieMiddleware(IDataProtectionProvider dataProtectionProvider, ILogger<AuthenticationCookieMiddleware> logger)
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
        var refreshTokenExpires = JsonSerializer.Deserialize<RefreshToken>(refreshToken)!.Expires;
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

    private record AuthenticationTokenPair(string RefreshToken, string AccessToken);
}
