using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.IdentityModel.Tokens;
using PlatformPlatform.SharedKernel.ApiCore;
using PlatformPlatform.SharedKernel.ApplicationCore.Authentication;

namespace PlatformPlatform.AppGateway.Middleware;

public class AuthenticationCookieMiddleware(
    IDataProtectionProvider dataProtectionProvider,
    AuthenticationTokenSettings authenticationTokenSettings,
    IHttpClientFactory httpClientFactory,
    ILogger<AuthenticationCookieMiddleware> logger
)
    : IMiddleware
{
    private const string? RefreshAuthenticationTokensEndpoint = "/api/account-management/authentication/refresh-authentication-tokens";
    private readonly IDataProtector _protector = dataProtectionProvider.CreateProtector("authentication-cookie");

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        if (context.Request.Cookies.TryGetValue(AuthenticationTokenSettings.AuthenticationCookieName, out var authenticationCookieValue))
        {
            await ValidateAuthenticationCookieAndConvertToHttpBearerHeader(context, authenticationCookieValue);
        }

        await next(context);

        if (context.Response.Headers.TryGetValue(AuthenticationTokenSettings.RefreshTokenHttpHeaderKey, out var refreshToken) &&
            context.Response.Headers.TryGetValue(AuthenticationTokenSettings.AccessTokenHttpHeaderKey, out var accessToken))
        {
            ReplaceAuthenticationHeaderWithCookie(context, refreshToken.Single()!, accessToken.Single()!);
        }
    }

    private async Task ValidateAuthenticationCookieAndConvertToHttpBearerHeader(HttpContext context, string authenticationCookieValue)
    {
        if (context.Request.Headers.ContainsKey(AuthenticationTokenSettings.RefreshTokenHttpHeaderKey) ||
            context.Request.Headers.ContainsKey(AuthenticationTokenSettings.AccessTokenHttpHeaderKey))
        {
            // The authentication cookie is used by WebApp, but API requests should use tokens in the headers
            throw new InvalidOperationException("A request cannot contain both an authentication cookie and security tokens in the headers.");
        }

        try
        {
            var authenticationTokenPair = Decrypt(authenticationCookieValue);

            if (ExtractExpirationFromToken(authenticationTokenPair.AccessToken) < TimeProvider.System.GetUtcNow())
            {
                if (ExtractExpirationFromToken(authenticationTokenPair.RefreshToken) < TimeProvider.System.GetUtcNow())
                {
                    context.Response.Cookies.Delete(AuthenticationTokenSettings.AuthenticationCookieName);
                    logger.LogDebug("The refresh-token has expired. The authentication cookie is removed.");
                    return;
                }

                authenticationTokenPair = await RefreshAuthenticationTokensAsync(authenticationTokenPair.RefreshToken);

                // Update the authentication cookie with the new tokens
                ReplaceAuthenticationHeaderWithCookie(context, authenticationTokenPair.RefreshToken, authenticationTokenPair.AccessToken);
            }

            context.Request.Headers["Authorization"] = $"Bearer {authenticationTokenPair.AccessToken}";
            if (context.Request.Path.Value == RefreshAuthenticationTokensEndpoint)
            {
                // When calling the refresh endpoint, use the refresh token as Bearer
                context.Request.Headers.Authorization = $"Bearer {authenticationTokenPair.RefreshToken}";
            }
            else
            {
                context.Request.Headers.Authorization = $"Bearer {authenticationTokenPair.AccessToken}";
            }
        }
        catch (SecurityTokenException ex)
        {
            context.Response.Cookies.Delete(AuthenticationTokenSettings.AuthenticationCookieName);
            logger.LogWarning(ex, "Validating or refreshing the authentication cookie tokens failed. {Message}", ex.Message);
        }
    }

    private async Task<AuthenticationTokenPair> RefreshAuthenticationTokensAsync(string refreshToken)
    {
        logger.LogDebug("The access-token has expired, attempting to refresh...");

        var request = new HttpRequestMessage(HttpMethod.Post, RefreshAuthenticationTokensEndpoint);

        // Use refresh Token as Bearer when refreshing Access Token
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", refreshToken);

        var accountManagmentHttpClient = httpClientFactory.CreateClient("AccountManagement");
        var response = await accountManagmentHttpClient.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            throw new SecurityTokenException($"Failed to refresh security tokens. Response status code: {response.StatusCode}");
        }

        var newRefreshToken = response.Headers.GetValues(AuthenticationTokenSettings.RefreshTokenHttpHeaderKey).SingleOrDefault();
        var newAccessToken = response.Headers.GetValues(AuthenticationTokenSettings.AccessTokenHttpHeaderKey).SingleOrDefault();

        if (newRefreshToken is null || newAccessToken is null)
        {
            throw new SecurityTokenException("Failed to get refreshed security tokens from the response.");
        }

        return new AuthenticationTokenPair(newRefreshToken, newAccessToken);
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
        context.Response.Cookies.Append(AuthenticationTokenSettings.AuthenticationCookieName, encryptedToken, cookieOptions);

        context.Response.Headers.Remove(AuthenticationTokenSettings.RefreshTokenHttpHeaderKey);
        context.Response.Headers.Remove(AuthenticationTokenSettings.AccessTokenHttpHeaderKey);
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

    private DateTimeOffset ExtractExpirationFromToken(string token)
    {
        var tokenHandler = new JwtSecurityTokenHandler();

        if (!tokenHandler.CanReadToken(token))
        {
            throw new SecurityTokenMalformedException("The token is not a valid JWT.");
        }

        var validationParameters = ApiCoreConfiguration.GetTokenValidationParameters(
            authenticationTokenSettings.Issuer,
            authenticationTokenSettings.Audience,
            authenticationTokenSettings.GetKeyBytes(),
            validateLifetime: false, // We validate the lifetime manually
            clockSkew: TimeSpan.FromSeconds(2) // In Azure we don't need clock skew, but this must be a lower value than in downstream APIs
        );

        // This will throw if the token is invalid
        var tokenClaims = tokenHandler.ValidateToken(token, validationParameters, out _);

        // The 'exp' claim is the number of seconds since Unix epoch (00:00:00 UTC on 1st January 1970)
        var expires = tokenClaims.FindFirstValue(JwtRegisteredClaimNames.Exp)!;

        return DateTimeOffset.FromUnixTimeSeconds(long.Parse(expires));
    }

    private sealed record AuthenticationTokenPair(string RefreshToken, string AccessToken);
}
