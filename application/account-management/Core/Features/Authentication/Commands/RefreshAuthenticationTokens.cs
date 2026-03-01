using System.Security.Claims;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.JsonWebTokens;
using PlatformPlatform.AccountManagement.Features.Authentication.Domain;
using PlatformPlatform.AccountManagement.Features.Users.Domain;
using PlatformPlatform.AccountManagement.Features.Users.Shared;
using PlatformPlatform.SharedKernel.Authentication;
using PlatformPlatform.SharedKernel.Authentication.TokenGeneration;
using PlatformPlatform.SharedKernel.Cqrs;
using PlatformPlatform.SharedKernel.Domain;
using PlatformPlatform.SharedKernel.Telemetry;

namespace PlatformPlatform.AccountManagement.Features.Authentication.Commands;

[PublicAPI]
public sealed record RefreshAuthenticationTokensCommand : ICommand, IRequest<Result>;

public sealed class RefreshAuthenticationTokensHandler(
    IUserRepository userRepository,
    ISessionRepository sessionRepository,
    UserInfoFactory userInfoFactory,
    IHttpContextAccessor httpContextAccessor,
    AuthenticationTokenService authenticationTokenService,
    ITelemetryEventsCollector events,
    TimeProvider timeProvider,
    ILogger<RefreshAuthenticationTokensHandler> logger
) : IRequestHandler<RefreshAuthenticationTokensCommand, Result>
{
    private const string InvalidRefreshTokenMessage = "Invalid refresh token.";

    public async Task<Result> Handle(RefreshAuthenticationTokensCommand command, CancellationToken cancellationToken)
    {
        var httpContext = httpContextAccessor.HttpContext ?? throw new InvalidOperationException("HttpContext is null.");

        var invalidTokenHeaders = new Dictionary<string, string>
        {
            { AuthenticationTokenHttpKeys.UnauthorizedReasonHeaderKey, nameof(UnauthorizedReason.SessionNotFound) }
        };

        if (!UserId.TryParse(httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
        {
            logger.LogWarning("No valid 'sub' claim found in refresh token");
            return Result.Unauthorized(InvalidRefreshTokenMessage, responseHeaders: invalidTokenHeaders);
        }

        if (!SessionId.TryParse(httpContext.User.FindFirstValue("sid"), out var sessionId))
        {
            logger.LogWarning("No valid 'sid' claim found in refresh token");
            return Result.Unauthorized(InvalidRefreshTokenMessage, responseHeaders: invalidTokenHeaders);
        }

        if (!RefreshTokenJti.TryParse(httpContext.User.FindFirstValue(JwtRegisteredClaimNames.Jti), out var jti))
        {
            logger.LogWarning("No valid 'jti' claim found in refresh token");
            return Result.Unauthorized(InvalidRefreshTokenMessage, responseHeaders: invalidTokenHeaders);
        }

        if (!int.TryParse(httpContext.User.FindFirstValue("ver"), out var refreshTokenVersion))
        {
            logger.LogWarning("No valid 'ver' claim found in refresh token");
            return Result.Unauthorized(InvalidRefreshTokenMessage, responseHeaders: invalidTokenHeaders);
        }

        var expiresClaim = httpContext.User.FindFirstValue(JwtRegisteredClaimNames.Exp);
        if (expiresClaim is null)
        {
            logger.LogWarning("No 'exp' claim found in refresh token");
            return Result.Unauthorized(InvalidRefreshTokenMessage, responseHeaders: invalidTokenHeaders);
        }

        if (!long.TryParse(expiresClaim, out var expiresUnixSeconds))
        {
            logger.LogWarning("Invalid 'exp' claim format in refresh token");
            return Result.Unauthorized(InvalidRefreshTokenMessage, responseHeaders: invalidTokenHeaders);
        }

        var refreshTokenExpires = DateTimeOffset.FromUnixTimeSeconds(expiresUnixSeconds);
        var now = timeProvider.GetUtcNow();

        var session = await sessionRepository.GetByIdUnfilteredAsync(sessionId, cancellationToken);
        if (session is null)
        {
            logger.LogWarning("No session found for session id '{SessionId}'", sessionId);
            return Result.Unauthorized(InvalidRefreshTokenMessage, responseHeaders: invalidTokenHeaders);
        }

        if (session.IsRevoked)
        {
            logger.LogWarning("Session '{SessionId}' has been revoked with reason '{RevokedReason}'", session.Id, session.RevokedReason);
            var unauthorizedHeaders = new Dictionary<string, string>
            {
                { AuthenticationTokenHttpKeys.UnauthorizedReasonHeaderKey, session.RevokedReason?.ToString() ?? nameof(UnauthorizedReason.Revoked) }
            };
            return Result.Unauthorized("Session has been revoked.", responseHeaders: unauthorizedHeaders);
        }

        if (session.UserId != userId)
        {
            logger.LogWarning("Session user id '{SessionUserId}' does not match token user id '{TokenUserId}'", session.UserId, userId);
            return Result.Unauthorized(InvalidRefreshTokenMessage, responseHeaders: invalidTokenHeaders);
        }

        if (!session.IsRefreshTokenValid(jti, refreshTokenVersion, now))
        {
            logger.LogWarning(
                "Replay attack detected for session '{SessionId}'. Token JTI '{TokenJti}', current JTI '{CurrentJti}'. Token version '{TokenVersion}', current version '{CurrentVersion}'",
                session.Id, jti, session.RefreshTokenJti, refreshTokenVersion, session.RefreshTokenVersion
            );

            // Atomic revocation - only one concurrent request succeeds, but all return ReplayAttackDetected
            await sessionRepository.TryRevokeForReplayUnfilteredAsync(sessionId, now, cancellationToken);

            events.CollectEvent(new SessionReplayDetected(session.Id, refreshTokenVersion, session.RefreshTokenVersion));
            var unauthorizedHeaders = new Dictionary<string, string>
            {
                { AuthenticationTokenHttpKeys.UnauthorizedReasonHeaderKey, nameof(UnauthorizedReason.ReplayAttackDetected) }
            };
            return Result.Unauthorized("Invalid refresh token. Session has been revoked due to potential replay attack.", true, unauthorizedHeaders);
        }

        var user = await userRepository.GetByIdAsync(userId, cancellationToken);
        if (user is null)
        {
            logger.LogWarning("No user found with user id '{UserId}'", userId);
            return Result.Unauthorized($"No user found with user id '{userId}'.", responseHeaders: invalidTokenHeaders);
        }

        RefreshTokenJti tokenJti;
        int tokenVersion;

        if (jti == session.RefreshTokenJti && refreshTokenVersion == session.RefreshTokenVersion)
        {
            // Attempt atomic refresh via isolated connection - only one concurrent request can succeed.
            // TryRefreshAsync commits immediately via its own connection, independent of UnitOfWorkPipelineBehavior.
            var newJti = RefreshTokenJti.NewId();
            var refreshed = await sessionRepository.TryRefreshAsync(session.Id, jti, refreshTokenVersion, newJti, now, cancellationToken);

            if (refreshed)
            {
                // Atomic refresh succeeded - update User.LastSeenAt (committed by UnitOfWorkPipelineBehavior)
                user.UpdateLastSeen(now);
                userRepository.Update(user);
                tokenJti = newJti;
                tokenVersion = refreshTokenVersion + 1;
            }
            else
            {
                // Concurrent request refreshed session after our fetch - re-fetch for updated values.
                // Grace period via PreviousRefreshTokenJti ensures this request still succeeds.
                session = await sessionRepository.GetByIdUnfilteredAsync(session.Id, cancellationToken)
                          ?? throw new InvalidOperationException("Session revoked during refresh.");
                tokenJti = session.RefreshTokenJti;
                tokenVersion = session.RefreshTokenVersion;
            }
        }
        else
        {
            // Grace period request - token validated via PreviousRefreshTokenJti, use current session values
            tokenJti = session.RefreshTokenJti;
            tokenVersion = session.RefreshTokenVersion;
        }

        var userInfoResult = await userInfoFactory.CreateUserInfoAsync(user, session.Id, cancellationToken);
        if (!userInfoResult.IsSuccess)
        {
            logger.LogWarning("Failed to create user info for user '{UserId}': tenant has been deleted", userId);
            var unauthorizedHeaders = new Dictionary<string, string>
            {
                { AuthenticationTokenHttpKeys.UnauthorizedReasonHeaderKey, nameof(UnauthorizedReason.TenantDeleted) }
            };
            return Result.Unauthorized("Tenant has been deleted.", responseHeaders: unauthorizedHeaders);
        }

        authenticationTokenService.GenerateAuthenticationTokens(userInfoResult.Value!, session.Id, tokenJti, tokenVersion, refreshTokenExpires);

        return Result.Success();
    }
}
