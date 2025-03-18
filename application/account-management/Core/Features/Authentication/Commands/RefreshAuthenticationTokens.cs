using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using JetBrains.Annotations;
using Mapster;
using Microsoft.AspNetCore.Http;
using PlatformPlatform.AccountManagement.Features.Users.Domain;
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
    IHttpContextAccessor httpContextAccessor,
    AuthenticationTokenService authenticationTokenService,
    ITelemetryEventsCollector events,
    ILogger<RefreshAuthenticationTokensHandler> logger
) : IRequestHandler<RefreshAuthenticationTokensCommand, Result>
{
    public async Task<Result> Handle(RefreshAuthenticationTokensCommand command, CancellationToken cancellationToken)
    {
        var httpContext = httpContextAccessor.HttpContext ?? throw new InvalidOperationException("HttpContext is null.");

        // Claims are already validated by the authentication middleware, so any missing claim is a programming error
        if (!UserId.TryParse(httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
        {
            logger.LogWarning("No valid 'sub' claim found in refresh token");
            return Result.Unauthorized("Invalid refresh token");
        }

        if (!RefreshTokenId.TryParse(httpContext.User.FindFirstValue("rtid"), out var refreshTokenId))
        {
            logger.LogWarning("No valid 'rtid' claim found in refresh token");
            return Result.Unauthorized("Invalid refresh token");
        }

        if (!int.TryParse(httpContext.User.FindFirstValue("rtv"), out var refreshTokenVersion))
        {
            logger.LogWarning("No valid 'rtv' claim found in refresh token");
            return Result.Unauthorized("Invalid refresh token");
        }

        var jwtId = httpContext.User.FindFirstValue(JwtRegisteredClaimNames.Jti);
        if (jwtId is null)
        {
            logger.LogWarning("No 'jti' claim found in refresh token");
            return Result.Unauthorized("Invalid refresh token");
        }

        var expiresClaim = httpContext.User.FindFirstValue(JwtRegisteredClaimNames.Exp);
        if (expiresClaim is null)
        {
            logger.LogWarning("No 'exp' claim found in refresh token");
            return Result.Unauthorized("Invalid refresh token");
        }

        if (!long.TryParse(expiresClaim, out var expiresUnixSeconds))
        {
            logger.LogWarning("Invalid 'exp' claim format in refresh token");
            return Result.Unauthorized("Invalid refresh token");
        }

        var refreshTokenExpires = DateTimeOffset.FromUnixTimeSeconds(expiresUnixSeconds);

        var user = await userRepository.GetByIdAsync(userId, cancellationToken);
        if (user is null)
        {
            logger.LogWarning("No user found with user id {UserId} found", userId);
            return Result.Unauthorized($"No user found with user id {userId} found.");
        }

        // TODO: Check if the refreshTokenId exists in the database and if the jwtId and refreshTokenVersion are valid

        authenticationTokenService.RefreshAuthenticationTokens(user.Adapt<UserInfo>(), refreshTokenId, refreshTokenVersion, refreshTokenExpires);
        events.CollectEvent(new AuthenticationTokensRefreshed());

        return Result.Success();
    }
}
