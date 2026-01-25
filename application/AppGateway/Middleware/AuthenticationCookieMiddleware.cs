using System.Net;
using System.Net.Http.Headers;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using PlatformPlatform.SharedKernel.Authentication;
using PlatformPlatform.SharedKernel.Authentication.TokenSigning;

namespace PlatformPlatform.AppGateway.Middleware;

public class AuthenticationCookieMiddleware(
    ITokenSigningClient tokenSigningClient,
    IHttpClientFactory httpClientFactory,
    TimeProvider timeProvider,
    ILogger<AuthenticationCookieMiddleware> logger
) : IMiddleware
{
    private const string RefreshAuthenticationTokensEndpoint = "/internal-api/account/authentication/refresh-authentication-tokens";
    private const string UnauthorizedReasonItemKey = "UnauthorizedReason";

    private static readonly JsonWebTokenHandler TokenHandler = new();

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        if (context.Request.Cookies.TryGetValue(AuthenticationTokenHttpKeys.RefreshTokenCookieName, out var refreshTokenCookieValue))
        {
            context.Request.Cookies.TryGetValue(AuthenticationTokenHttpKeys.AccessTokenCookieName, out var accessTokenCookieValue);
            await ValidateAuthenticationCookieAndConvertToHttpBearerHeader(context, refreshTokenCookieValue, accessTokenCookieValue);
        }

        // If session was revoked during refresh, handle based on request type
        if (context.Items.TryGetValue(UnauthorizedReasonItemKey, out var reason) && reason is string unauthorizedReason)
        {
            if (context.Request.Path.StartsWithSegments("/api"))
            {
                // For API requests: return 401 immediately so JavaScript can handle it
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                context.Response.Headers[AuthenticationTokenHttpKeys.UnauthorizedReasonHeaderKey] = unauthorizedReason;
                return;
            }

            // For non-API requests (SPA routes): delete cookies and let the page load
            // The SPA will load without auth and redirect to login as needed
            var hostCookieOptions = new CookieOptions { Secure = true };
            context.Response.Cookies.Delete(AuthenticationTokenHttpKeys.RefreshTokenCookieName, hostCookieOptions);
            context.Response.Cookies.Delete(AuthenticationTokenHttpKeys.AccessTokenCookieName, hostCookieOptions);
        }

        await next(context);


        if (context.Response.Headers.TryGetValue(AuthenticationTokenHttpKeys.RefreshAuthenticationTokensHeaderKey, out _))
        {
            logger.LogDebug("Refreshing authentication tokens as requested by endpoint");
            var (refreshToken, accessToken) = await RefreshAuthenticationTokensAsync(refreshTokenCookieValue!);
            await ReplaceAuthenticationHeaderWithCookieAsync(context, refreshToken, accessToken);
            context.Response.Headers.Remove(AuthenticationTokenHttpKeys.RefreshAuthenticationTokensHeaderKey);
        }
        else if (context.Response.Headers.TryGetValue(AuthenticationTokenHttpKeys.RefreshTokenHttpHeaderKey, out var refreshToken) &&
                 context.Response.Headers.TryGetValue(AuthenticationTokenHttpKeys.AccessTokenHttpHeaderKey, out var accessToken))
        {
            await ReplaceAuthenticationHeaderWithCookieAsync(context, refreshToken.Single()!, accessToken.Single()!);
        }
    }

    private async Task ValidateAuthenticationCookieAndConvertToHttpBearerHeader(HttpContext context, string refreshToken, string? accessToken)
    {
        if (context.Request.Headers.ContainsKey(AuthenticationTokenHttpKeys.RefreshTokenHttpHeaderKey) ||
            context.Request.Headers.ContainsKey(AuthenticationTokenHttpKeys.AccessTokenHttpHeaderKey))
        {
            // The authentication token cookie is used by WebApp, but API requests should use tokens in the headers
            throw new InvalidOperationException("A request cannot contain both an authentication token cookies and security tokens in the headers.");
        }

        try
        {
            if (accessToken is null || await ExtractExpirationFromTokenAsync(accessToken) < timeProvider.GetUtcNow())
            {
                if (await ExtractExpirationFromTokenAsync(refreshToken) < timeProvider.GetUtcNow())
                {
                    var expiredCookieOptions = new CookieOptions { Secure = true };
                    context.Response.Cookies.Delete(AuthenticationTokenHttpKeys.RefreshTokenCookieName, expiredCookieOptions);
                    context.Response.Cookies.Delete(AuthenticationTokenHttpKeys.AccessTokenCookieName, expiredCookieOptions);
                    logger.LogDebug("The refresh-token has expired; authentication token cookies are removed");
                    return;
                }

                logger.LogDebug("The access-token has expired, attempting to refresh");

                (refreshToken, accessToken) = await RefreshAuthenticationTokensAsync(refreshToken);

                // Update the authentication token cookies with the new tokens
                await ReplaceAuthenticationHeaderWithCookieAsync(context, refreshToken, accessToken);
            }

            context.Request.Headers.Authorization = $"Bearer {accessToken}";
        }
        catch (SessionRevokedException ex)
        {
            DeleteCookiesForApiRequestsOnly(context);
            context.Items[UnauthorizedReasonItemKey] = ex.RevokedReason;
            logger.LogWarning(ex, "Session revoked during token refresh. Reason: {Reason}", ex.RevokedReason);
        }
        catch (SecurityTokenException ex)
        {
            DeleteCookiesForApiRequestsOnly(context);
            context.Items[UnauthorizedReasonItemKey] = nameof(UnauthorizedReason.SessionNotFound);
            logger.LogWarning(ex, "Validating or refreshing the authentication token cookies failed. {Message}", ex.Message);
        }
        catch (HttpRequestException ex)
        {
            // Backend temporarily unreachable (e.g., cold start, deployment). Preserve cookies so the
            // session recovers on the next request once the backend is available.
            logger.LogWarning(ex, "Backend unavailable during token refresh. Path: {Path}", context.Request.Path);
        }
        catch (TaskCanceledException ex) when (!context.RequestAborted.IsCancellationRequested)
        {
            // HTTP timeout waiting for backend (not a client disconnect). Preserve cookies so the
            // session recovers on the next request once the backend is available.
            logger.LogWarning(ex, "Backend timed out during token refresh. Path: {Path}", context.Request.Path);
        }
        catch (Exception ex)
        {
            DeleteCookiesForApiRequestsOnly(context);
            context.Items[UnauthorizedReasonItemKey] = nameof(UnauthorizedReason.SessionNotFound);
            logger.LogError(ex, "Unexpected exception during authentication token validation. Path: {Path}", context.Request.Path);
        }
    }

    private async Task<(string newRefreshToken, string newAccessToken)> RefreshAuthenticationTokensAsync(string refreshToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, RefreshAuthenticationTokensEndpoint);

        // Use refresh Token as Bearer when refreshing Access Token
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", refreshToken);

        var accountHttpClient = httpClientFactory.CreateClient("Account");
        var response = await accountHttpClient.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            var unauthorizedReason = GetUnauthorizedReason(response);
            if (unauthorizedReason is not null)
            {
                throw new SessionRevokedException(unauthorizedReason);
            }

            if (response.StatusCode is HttpStatusCode.BadGateway or HttpStatusCode.ServiceUnavailable or HttpStatusCode.GatewayTimeout)
            {
                throw new HttpRequestException($"Backend temporarily unavailable. Status: {response.StatusCode}.", null, response.StatusCode);
            }

            throw new SecurityTokenException($"Failed to refresh security tokens. Response status code: {response.StatusCode}.");
        }

        var newRefreshToken = response.Headers.GetValues(AuthenticationTokenHttpKeys.RefreshTokenHttpHeaderKey).SingleOrDefault();
        var newAccessToken = response.Headers.GetValues(AuthenticationTokenHttpKeys.AccessTokenHttpHeaderKey).SingleOrDefault();

        if (newRefreshToken is null || newAccessToken is null)
        {
            throw new SecurityTokenException("Failed to get refreshed security tokens from the response.");
        }

        return (newRefreshToken, newAccessToken);
    }

    private static string? GetUnauthorizedReason(HttpResponseMessage response)
    {
        if (response.Headers.TryGetValues(AuthenticationTokenHttpKeys.UnauthorizedReasonHeaderKey, out var values))
        {
            return values.FirstOrDefault();
        }

        return null;
    }

    /// <summary>
    ///     Only delete authentication cookies for API requests. For non-API requests (images, static assets),
    ///     keep the cookies so subsequent API requests can properly detect session issues like replay attacks.
    ///     The frontend's AuthenticationMiddleware only intercepts API responses, not image/asset errors.
    /// </summary>
    private static void DeleteCookiesForApiRequestsOnly(HttpContext context)
    {
        if (!context.Request.Path.StartsWithSegments("/api"))
        {
            return;
        }

        var hostCookieOptions = new CookieOptions { Secure = true };
        context.Response.Cookies.Delete(AuthenticationTokenHttpKeys.RefreshTokenCookieName, hostCookieOptions);
        context.Response.Cookies.Delete(AuthenticationTokenHttpKeys.AccessTokenCookieName, hostCookieOptions);
    }

    private async Task ReplaceAuthenticationHeaderWithCookieAsync(HttpContext context, string refreshToken, string accessToken)
    {
        var refreshTokenExpires = await ExtractExpirationFromTokenAsync(refreshToken);

        // The refresh token cookie is SameSiteMode.Lax, which makes the cookie available on the first request when redirected
        // from another site. This means we can redirect to the login page if the user is not authenticated without
        // having to first serve the SPA. This is only secure if iFrames are not allowed to host the site.
        var refreshTokenCookieOptions = new CookieOptions
        {
            HttpOnly = true, Secure = true, SameSite = SameSiteMode.Lax, Expires = refreshTokenExpires
        };
        context.Response.Cookies.Append(AuthenticationTokenHttpKeys.RefreshTokenCookieName, refreshToken, refreshTokenCookieOptions);

        var accessTokenCookieOptions = new CookieOptions { HttpOnly = true, Secure = true, SameSite = SameSiteMode.Strict };
        context.Response.Cookies.Append(AuthenticationTokenHttpKeys.AccessTokenCookieName, accessToken, accessTokenCookieOptions);

        context.Response.Headers.Remove(AuthenticationTokenHttpKeys.RefreshTokenHttpHeaderKey);
        context.Response.Headers.Remove(AuthenticationTokenHttpKeys.AccessTokenHttpHeaderKey);
    }

    private async Task<DateTimeOffset> ExtractExpirationFromTokenAsync(string token)
    {
        if (!TokenHandler.CanReadToken(token))
        {
            throw new SecurityTokenMalformedException("The token is not a valid JWT.");
        }

        var validationParameters = tokenSigningClient.GetTokenValidationParameters(
            validateLifetime: false, // We validate the lifetime manually
            clockSkew: TimeSpan.FromSeconds(2) // In Azure, we don't need any clock skew, but this must be a lower value than in downstream APIs
        );

        var validationResult = await TokenHandler.ValidateTokenAsync(token, validationParameters);

        if (!validationResult.IsValid)
        {
            throw validationResult.Exception;
        }

        // The 'exp' claim is the number of seconds since Unix epoch (00:00:00 UTC on 1st January 1970)
        var expires = validationResult.Claims[JwtRegisteredClaimNames.Exp]?.ToString()!;

        return DateTimeOffset.FromUnixTimeSeconds(long.Parse(expires));
    }
}

public sealed class SessionRevokedException(string revokedReason) : SecurityTokenException($"Session has been revoked. Reason: {revokedReason}")
{
    public string RevokedReason { get; } = revokedReason;
}
