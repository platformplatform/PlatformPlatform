using System.Net;
using System.Net.Http.Headers;
using AppGateway.Middleware;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using SharedKernel.Authentication;
using SharedKernel.Authentication.TokenSigning;
using Yarp.ReverseProxy.Transforms;

namespace AppGateway.Transformations;

/// <summary>
///     YARP response transform that consolidates the post-upstream authentication response handling:
///     swap any header-mode refresh/access tokens into cookies, perform the endpoint-triggered token
///     refresh when the upstream sets <c>x-refresh-authentication-tokens-required</c>, and emit
///     <c>x-user-feature-flags</c> from the (post-refresh) access-token claim.
///     A single hook eliminates the race the prior split design had: a separate OnStarting callback
///     refreshed the cookies AFTER this transform had already read the pre-refresh access token,
///     causing the actor's same-response header to reflect stale flag state. The transform runs
///     after the upstream response is received and before YARP calls <c>Response.StartAsync</c>,
///     so all mutations land on the same outgoing response.
/// </summary>
public sealed class UserFeatureFlagsResponseTransform(
    IHttpClientFactory httpClientFactory,
    ITokenSigningClient tokenSigningClient,
    ILogger<UserFeatureFlagsResponseTransform> logger
) : ResponseTransform
{
    public const string CurrentAccessTokenItemKey = "CurrentAccessToken";
    public const string InboundRefreshTokenItemKey = "InboundRefreshToken";

    private const string RefreshAuthenticationTokensEndpoint = "/internal-api/account/authentication/refresh-authentication-tokens";

    private static readonly JsonWebTokenHandler TokenHandler = new();

    public override async ValueTask ApplyAsync(ResponseTransformContext context)
    {
        var httpContext = context.HttpContext;

        // Endpoint-triggered refresh: the downstream signaled the actor's claims have changed
        // (e.g. PUT /me, PUT /me/change-locale, PUT /api/account/feature-flags/{key}/tenant-override).
        // Refresh the JWT now so the same response carries fresh cookies AND a fresh x-user-feature-flags.
        if (httpContext.Response.Headers.TryGetValue(AuthenticationTokenHttpKeys.RefreshAuthenticationTokensHeaderKey, out _))
        {
            if (httpContext.Items.TryGetValue(InboundRefreshTokenItemKey, out var refreshItem) && refreshItem is string inboundRefreshToken)
            {
                try
                {
                    logger.LogDebug("Refreshing authentication tokens as requested by endpoint");
                    var (refreshToken, accessToken) = await RefreshAuthenticationTokensAsync(inboundRefreshToken);
                    await ReplaceAuthenticationHeaderWithCookieAsync(httpContext, refreshToken, accessToken);
                }
                catch (SessionRevokedException ex)
                {
                    OverwriteWithUnauthorized(httpContext, ex.RevokedReason);
                    logger.LogWarning(ex, "Session revoked during endpoint-triggered refresh. Reason: {Reason}", ex.RevokedReason);
                    return;
                }
                catch (SecurityTokenException ex)
                {
                    OverwriteWithUnauthorized(httpContext, nameof(UnauthorizedReason.SessionNotFound));
                    logger.LogWarning(ex, "Endpoint-triggered token refresh failed validation. Path: {Path}", httpContext.Request.Path);
                    return;
                }
                catch (HttpRequestException ex)
                {
                    // Backend temporarily unreachable: the upstream mutation already succeeded, so
                    // let the response through. The SPA picks up new claims on the next refresh.
                    logger.LogWarning(ex, "Backend unavailable during endpoint-triggered refresh. Path: {Path}", httpContext.Request.Path);
                }
                catch (TaskCanceledException ex) when (!httpContext.RequestAborted.IsCancellationRequested)
                {
                    logger.LogWarning(ex, "Backend timed out during endpoint-triggered refresh. Path: {Path}", httpContext.Request.Path);
                }
            }

            httpContext.Response.Headers.Remove(AuthenticationTokenHttpKeys.RefreshAuthenticationTokensHeaderKey);
        }
        else if (httpContext.Response.Headers.TryGetValue(AuthenticationTokenHttpKeys.RefreshTokenHttpHeaderKey, out var refreshToken) &&
                 httpContext.Response.Headers.TryGetValue(AuthenticationTokenHttpKeys.AccessTokenHttpHeaderKey, out var accessToken))
        {
            // Login / signup / switch-tenant flows return the new tokens in response headers
            // for AppGateway to convert into cookies before egress.
            await ReplaceAuthenticationHeaderWithCookieAsync(httpContext, refreshToken.Single()!, accessToken.Single()!);
        }

        // Emit x-user-feature-flags from the current access token (post-refresh if a refresh just
        // happened above, else the inbound token stashed by AuthenticationCookieMiddleware).
        if (httpContext.Items.TryGetValue(CurrentAccessTokenItemKey, out var tokenItem) && tokenItem is string currentAccessToken)
        {
            httpContext.Response.Headers[AuthenticationTokenHttpKeys.UserFeatureFlagsHeaderKey] = ExtractFeatureFlagsClaim(currentAccessToken);
        }
    }

    private async Task<(string newRefreshToken, string newAccessToken)> RefreshAuthenticationTokensAsync(string refreshToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, RefreshAuthenticationTokensEndpoint);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", refreshToken);

        var accountHttpClient = httpClientFactory.CreateClient("Account");
        var response = await accountHttpClient.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            if (response.Headers.TryGetValues(AuthenticationTokenHttpKeys.UnauthorizedReasonHeaderKey, out var reasons) && reasons.FirstOrDefault() is { } revokedReason)
            {
                throw new SessionRevokedException(revokedReason);
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

    private async Task ReplaceAuthenticationHeaderWithCookieAsync(HttpContext context, string refreshToken, string accessToken)
    {
        var refreshTokenExpires = await ExtractExpirationFromTokenAsync(refreshToken);

        var refreshTokenCookieOptions = new CookieOptions
        {
            HttpOnly = true, Secure = true, SameSite = SameSiteMode.Lax, Expires = refreshTokenExpires, Path = "/"
        };
        context.Response.Cookies.Append(AuthenticationTokenHttpKeys.RefreshTokenCookieName, refreshToken, refreshTokenCookieOptions);

        var accessTokenCookieOptions = new CookieOptions { HttpOnly = true, Secure = true, SameSite = SameSiteMode.Strict, Path = "/" };
        context.Response.Cookies.Append(AuthenticationTokenHttpKeys.AccessTokenCookieName, accessToken, accessTokenCookieOptions);

        context.Response.Headers.Remove(AuthenticationTokenHttpKeys.RefreshTokenHttpHeaderKey);
        context.Response.Headers.Remove(AuthenticationTokenHttpKeys.AccessTokenHttpHeaderKey);

        context.Items[CurrentAccessTokenItemKey] = accessToken;
    }

    private async Task<DateTimeOffset> ExtractExpirationFromTokenAsync(string token)
    {
        if (!TokenHandler.CanReadToken(token))
        {
            throw new SecurityTokenMalformedException("The token is not a valid JWT.");
        }

        var validationParameters = tokenSigningClient.GetTokenValidationParameters(
            validateLifetime: false,
            clockSkew: TimeSpan.FromSeconds(2)
        );

        var validationResult = await TokenHandler.ValidateTokenAsync(token, validationParameters);

        if (!validationResult.IsValid)
        {
            throw validationResult.Exception;
        }

        var expires = validationResult.Claims[JwtRegisteredClaimNames.Exp]?.ToString()!;
        return DateTimeOffset.FromUnixTimeSeconds(long.Parse(expires));
    }

    private static string ExtractFeatureFlagsClaim(string accessToken)
    {
        if (!TokenHandler.CanReadToken(accessToken)) return string.Empty;
        var jwt = TokenHandler.ReadJsonWebToken(accessToken);
        return jwt.TryGetClaim("feature_flags", out var claim) ? claim.Value : string.Empty;
    }

    /// <summary>
    ///     Convert the upstream success response into a 401 with <c>x-unauthorized-reason</c> so the
    ///     SPA's authentication middleware can react (redirect to login, surface revocation reason).
    ///     Mirrors the pre-consolidation behavior in <see cref="AuthenticationCookieMiddleware" />.
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
}
