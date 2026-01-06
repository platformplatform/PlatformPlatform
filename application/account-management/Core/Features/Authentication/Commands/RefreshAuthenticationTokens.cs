using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Http;
using PlatformPlatform.AccountManagement.Features.Authentication.Domain;
using PlatformPlatform.AccountManagement.Features.Users.Domain;
using PlatformPlatform.AccountManagement.Features.Users.Shared;
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
    public async Task<Result> Handle(RefreshAuthenticationTokensCommand command, CancellationToken cancellationToken)
    {
        var httpContext = httpContextAccessor.HttpContext ?? throw new InvalidOperationException("HttpContext is null.");

        if (!UserId.TryParse(httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
        {
            logger.LogWarning("No valid 'sub' claim found in refresh token");
            return Result.Unauthorized("Invalid refresh token.");
        }

        if (!SessionId.TryParse(httpContext.User.FindFirstValue("sid"), out var sessionId))
        {
            logger.LogWarning("No valid 'sid' claim found in refresh token");
            return Result.Unauthorized("Invalid refresh token.");
        }

        if (!RefreshTokenJti.TryParse(httpContext.User.FindFirstValue(JwtRegisteredClaimNames.Jti), out var jti))
        {
            logger.LogWarning("No valid 'jti' claim found in refresh token");
            return Result.Unauthorized("Invalid refresh token.");
        }

        if (!int.TryParse(httpContext.User.FindFirstValue("ver"), out var refreshTokenVersion))
        {
            logger.LogWarning("No valid 'ver' claim found in refresh token");
            return Result.Unauthorized("Invalid refresh token.");
        }

        var expiresClaim = httpContext.User.FindFirstValue(JwtRegisteredClaimNames.Exp);
        if (expiresClaim is null)
        {
            logger.LogWarning("No 'exp' claim found in refresh token");
            return Result.Unauthorized("Invalid refresh token.");
        }

        if (!long.TryParse(expiresClaim, out var expiresUnixSeconds))
        {
            logger.LogWarning("Invalid 'exp' claim format in refresh token");
            return Result.Unauthorized("Invalid refresh token.");
        }

        var refreshTokenExpires = DateTimeOffset.FromUnixTimeSeconds(expiresUnixSeconds);
        var now = timeProvider.GetUtcNow();

        var session = await sessionRepository.GetByIdUnfilteredAsync(sessionId, cancellationToken);
        if (session is null)
        {
            logger.LogWarning("No session found for session id '{SessionId}'", sessionId);
            return Result.Unauthorized("Invalid refresh token.");
        }

        if (session.IsRevoked)
        {
            logger.LogWarning("Session '{SessionId}' has been revoked", session.Id);
            return Result.Unauthorized("Session has been revoked.");
        }

        if (session.UserId != userId)
        {
            logger.LogWarning("Session user id '{SessionUserId}' does not match token user id '{TokenUserId}'", session.UserId, userId);
            return Result.Unauthorized("Invalid refresh token.");
        }

        if (!session.IsRefreshTokenValid(jti, refreshTokenVersion, now))
        {
            logger.LogWarning("Replay attack detected for session '{SessionId}'. Token JTI '{TokenJti}', current JTI '{CurrentJti}'. Token version '{TokenVersion}', current version '{CurrentVersion}'", session.Id, jti, session.RefreshTokenJti, refreshTokenVersion, session.RefreshTokenVersion);
            session.Revoke(now, SessionRevokedReason.ReplayAttackDetected);
            sessionRepository.Update(session);
            events.CollectEvent(new SessionReplayDetected(session.Id, refreshTokenVersion, session.RefreshTokenVersion));
            return Result.Unauthorized("Invalid refresh token. Session has been revoked due to potential replay attack.", true);
        }

        var user = await userRepository.GetByIdAsync(userId, cancellationToken);
        if (user is null)
        {
            logger.LogWarning("No user found with user id '{UserId}'", userId);
            return Result.Unauthorized($"No user found with user id '{userId}'.");
        }

        if (jti == session.RefreshTokenJti && refreshTokenVersion == session.RefreshTokenVersion)
        {
            session.Refresh();
            sessionRepository.Update(session);

            user.UpdateLastSeen(now);
            userRepository.Update(user);
        }

        var userInfo = await userInfoFactory.CreateUserInfoAsync(user, cancellationToken, session.Id);
        authenticationTokenService.RefreshAuthenticationTokens(userInfo, session.Id, session.RefreshTokenJti, refreshTokenVersion, refreshTokenExpires);

        events.CollectEvent(new SessionRefreshed(session.Id));
        events.CollectEvent(new AuthenticationTokensRefreshed());

        return Result.Success();
    }
}
