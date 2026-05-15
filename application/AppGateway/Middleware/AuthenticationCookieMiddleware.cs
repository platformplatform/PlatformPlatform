using System.Net;
using System.Net.Http.Headers;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using SharedKernel.Authentication;
using SharedKernel.Authentication.TokenSigning;

namespace AppGateway.Middleware;

public sealed class AuthenticationCookieMiddleware(
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
        var tokenState = new TokenState();

        if (context.Request.Cookies.TryGetValue(AuthenticationTokenHttpKeys.RefreshTokenCookieName, out var refreshTokenFromCookie))
        {
            tokenState.InboundRefreshToken = refreshTokenFromCookie;
            context.Request.Cookies.TryGetValue(AuthenticationTokenHttpKeys.AccessTokenCookieName, out var accessTokenCookieValue);
            tokenState.CurrentAccessToken = await ValidateAuthenticationCookieAndConvertToHttpBearerHeader(context, tokenState, accessTokenCookieValue);
        }

        // If session was revoked during the inbound cookie validation, short-circuit before reaching
        // downstream. The OnStarting callback registered below will still emit x-user-feature-flags if
        // a current token exists; in this revoked path we return early without a token, so no header.
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
            var hostCookieOptions = new CookieOptions { Secure = true, Path = "/" };
            context.Response.Cookies.Delete(AuthenticationTokenHttpKeys.RefreshTokenCookieName, hostCookieOptions);
            context.Response.Cookies.Delete(AuthenticationTokenHttpKeys.AccessTokenCookieName, hostCookieOptions);
        }

        // Single OnStarting hook for everything driven by the downstream response: header-mode
        // cookie swap (login / signup / switch-tenant), endpoint-triggered refresh
        // (x-refresh-authentication-tokens-required), session-revoked 401 override, and
        // x-user-feature-flags emission from the current (possibly just-refreshed) access token.
        // Sequential execution in one hook eliminates the race the split YARP-transform design had.
        context.Response.OnStarting(async state =>
            {
                var (httpContext, currentState) = ((HttpContext, TokenState))state;
                await HandleOutgoingResponseAsync(httpContext, currentState);
            }, (context, tokenState)
        );

        await next(context);
    }

    private async Task HandleOutgoingResponseAsync(HttpContext context, TokenState tokenState)
    {
        if (context.Response.Headers.TryGetValue(AuthenticationTokenHttpKeys.RefreshAuthenticationTokensHeaderKey, out _))
        {
            // Endpoint-triggered refresh: the downstream signaled the actor's claims have changed
            // (e.g. PUT /me, PUT /me/change-locale, PUT /api/account/feature-flags/{key}/tenant-override).
            // Refresh the JWT now so the same response carries fresh cookies AND a fresh x-user-feature-flags.
            if (tokenState.InboundRefreshToken is { } refreshToken)
            {
                try
                {
                    logger.LogDebug("Refreshing authentication tokens as requested by endpoint");
                    var (newRefreshToken, newAccessToken) = await RefreshAuthenticationTokensAsync(refreshToken);
                    await ReplaceAuthenticationHeaderWithCookieAsync(context, newRefreshToken, newAccessToken);
                    tokenState.CurrentAccessToken = newAccessToken;
                }
                catch (SessionRevokedException ex)
                {
                    OverwriteWithUnauthorized(context, ex.RevokedReason);
                    logger.LogWarning(ex, "Session revoked during endpoint-triggered refresh. Reason: {Reason}", ex.RevokedReason);
                    return;
                }
                catch (SecurityTokenException ex)
                {
                    OverwriteWithUnauthorized(context, nameof(UnauthorizedReason.SessionNotFound));
                    logger.LogWarning(ex, "Endpoint-triggered token refresh failed validation. Path: {Path}", context.Request.Path);
                    return;
                }
                catch (HttpRequestException ex)
                {
                    // Backend temporarily unreachable: the upstream mutation already succeeded, so
                    // let the response through. The SPA picks up new claims on the next refresh. The
                    // degraded flag below suppresses x-user-feature-flags emission so the SPA isn't
                    // told the pre-mutation flag set is current — that would look like the mutation
                    // didn't take effect.
                    logger.LogWarning(ex, "Backend unavailable during endpoint-triggered refresh. Path: {Path}", context.Request.Path);
                    tokenState.EndpointTriggeredRefreshFailedDegraded = true;
                }
                catch (TaskCanceledException ex) when (!context.RequestAborted.IsCancellationRequested)
                {
                    logger.LogWarning(ex, "Backend timed out during endpoint-triggered refresh. Path: {Path}", context.Request.Path);
                    tokenState.EndpointTriggeredRefreshFailedDegraded = true;
                }
            }

            context.Response.Headers.Remove(AuthenticationTokenHttpKeys.RefreshAuthenticationTokensHeaderKey);
        }
        else if (context.Response.Headers.TryGetValue(AuthenticationTokenHttpKeys.RefreshTokenHttpHeaderKey, out var refreshTokenHeader) &&
                 context.Response.Headers.TryGetValue(AuthenticationTokenHttpKeys.AccessTokenHttpHeaderKey, out var accessTokenHeader))
        {
            // Login / signup / switch-tenant flows return the new tokens in response headers
            // for AppGateway to convert into cookies before egress.
            var newRefreshToken = refreshTokenHeader.Single()!;
            var newAccessToken = accessTokenHeader.Single()!;
            await ReplaceAuthenticationHeaderWithCookieAsync(context, newRefreshToken, newAccessToken);
            tokenState.CurrentAccessToken = newAccessToken;
        }

        if (tokenState is { EndpointTriggeredRefreshFailedDegraded: false, CurrentAccessToken: { } currentAccessToken } &&
            ExtractFeatureFlagsClaim(currentAccessToken) is { } featureFlagsClaim)
        {
            context.Response.Headers[AuthenticationTokenHttpKeys.UserFeatureFlagsHeaderKey] = featureFlagsClaim;
        }
    }

    private async Task<string?> ValidateAuthenticationCookieAndConvertToHttpBearerHeader(HttpContext context, TokenState tokenState, string? accessToken)
    {
        if (context.Request.Headers.ContainsKey(AuthenticationTokenHttpKeys.RefreshTokenHttpHeaderKey) ||
            context.Request.Headers.ContainsKey(AuthenticationTokenHttpKeys.AccessTokenHttpHeaderKey))
        {
            // The authentication token cookie is used by WebApp, but API requests should use tokens in the headers
            throw new InvalidOperationException("A request cannot contain both an authentication token cookies and security tokens in the headers.");
        }

        try
        {
            var refreshToken = tokenState.InboundRefreshToken!;

            if (accessToken is null || await ExtractExpirationFromTokenAsync(accessToken) < timeProvider.GetUtcNow())
            {
                if (await ExtractExpirationFromTokenAsync(refreshToken) < timeProvider.GetUtcNow())
                {
                    var expiredCookieOptions = new CookieOptions { Secure = true, Path = "/" };
                    context.Response.Cookies.Delete(AuthenticationTokenHttpKeys.RefreshTokenCookieName, expiredCookieOptions);
                    context.Response.Cookies.Delete(AuthenticationTokenHttpKeys.AccessTokenCookieName, expiredCookieOptions);
                    logger.LogDebug("The refresh-token has expired; authentication token cookies are removed");
                    return null;
                }

                logger.LogDebug("The access-token has expired, attempting to refresh");

                (refreshToken, accessToken) = await RefreshAuthenticationTokensAsync(refreshToken);

                // Mirror the rotated refresh token onto tokenState so a downstream-triggered refresh in
                // HandleOutgoingResponseAsync uses the v=2 jti, not the stale v=1 cookie value. Without
                // this the second refresh fell back to the 30-second grace window in Session.IsRefreshTokenValid
                // and would emit a spurious 401 once the gap exceeded the grace window.
                tokenState.InboundRefreshToken = refreshToken;

                // Update the authentication token cookies with the new tokens
                await ReplaceAuthenticationHeaderWithCookieAsync(context, refreshToken, accessToken);
            }

            context.Request.Headers.Authorization = $"Bearer {accessToken}";
            return accessToken;
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

        return null;
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

        var hostCookieOptions = new CookieOptions { Secure = true, Path = "/" };
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
            HttpOnly = true, Secure = true, SameSite = SameSiteMode.Lax, Expires = refreshTokenExpires, Path = "/"
        };
        context.Response.Cookies.Append(AuthenticationTokenHttpKeys.RefreshTokenCookieName, refreshToken, refreshTokenCookieOptions);

        var accessTokenCookieOptions = new CookieOptions { HttpOnly = true, Secure = true, SameSite = SameSiteMode.Strict, Path = "/" };
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

    private static string? ExtractFeatureFlagsClaim(string accessToken)
    {
        if (!TokenHandler.CanReadToken(accessToken)) return null;
        var jwt = TokenHandler.ReadJsonWebToken(accessToken);
        return jwt.TryGetClaim(AuthenticationTokenHttpKeys.FeatureFlagsClaimName, out var claim) ? claim.Value : null;
    }

    /// <summary>
    ///     Convert the upstream success response into a 401 with <c>x-unauthorized-reason</c> so the
    ///     SPA's authentication middleware can react (redirect to login, surface revocation reason).
    /// </summary>
    private static void OverwriteWithUnauthorized(HttpContext context, string unauthorizedReason)
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        context.Response.Headers[AuthenticationTokenHttpKeys.UnauthorizedReasonHeaderKey] = unauthorizedReason;
        context.Response.Headers.Remove(AuthenticationTokenHttpKeys.RefreshAuthenticationTokensHeaderKey);
        context.Response.Headers.Remove(AuthenticationTokenHttpKeys.RefreshTokenHttpHeaderKey);
        context.Response.Headers.Remove(AuthenticationTokenHttpKeys.AccessTokenHttpHeaderKey);

        var hostCookieOptions = new CookieOptions { Secure = true, Path = "/" };
        context.Response.Cookies.Delete(AuthenticationTokenHttpKeys.RefreshTokenCookieName, hostCookieOptions);
        context.Response.Cookies.Delete(AuthenticationTokenHttpKeys.AccessTokenCookieName, hostCookieOptions);
    }

    private sealed class TokenState
    {
        public string? CurrentAccessToken { get; set; }

        public string? InboundRefreshToken { get; set; }

        // Set when an endpoint-triggered refresh swallows a transient backend failure so the response
        // does NOT emit x-user-feature-flags from the now-stale pre-refresh access token. Without this,
        // the SPA would interpret a successful mutation + stale claim header as "the toggle didn't
        // apply", causing flag-toggle UX confusion.
        public bool EndpointTriggeredRefreshFailedDegraded { get; set; }
    }
}

public sealed class SessionRevokedException(string revokedReason) : SecurityTokenException($"Session has been revoked. Reason: {revokedReason}")
{
    public string RevokedReason { get; } = revokedReason;
}
