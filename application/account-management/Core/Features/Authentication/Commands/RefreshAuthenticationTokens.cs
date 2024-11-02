using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Http;
using PlatformPlatform.AccountManagement.Features.Authentication.Services;
using PlatformPlatform.AccountManagement.Features.Users.Domain;
using PlatformPlatform.AccountManagement.TelemetryEvents;
using PlatformPlatform.SharedKernel.Cqrs;
using PlatformPlatform.SharedKernel.Domain;
using PlatformPlatform.SharedKernel.TelemetryEvents;

namespace PlatformPlatform.AccountManagement.Features.Authentication.Commands;

[PublicAPI]
public sealed record RefreshAuthenticationTokensCommand : ICommand, IRequest<Result>;

public sealed class RefreshAuthenticationTokensCommandHandler(
    IUserRepository userRepository,
    IHttpContextAccessor httpContextAccessor,
    AuthenticationTokenService authenticationTokenService,
    ITelemetryEventsCollector events,
    ILogger<RefreshAuthenticationTokensCommandHandler> logger
) : IRequestHandler<RefreshAuthenticationTokensCommand, Result>
{
    public async Task<Result> Handle(RefreshAuthenticationTokensCommand command, CancellationToken cancellationToken)
    {
        var httpContext = httpContextAccessor.HttpContext ?? throw new InvalidOperationException("HttpContext is null.");

        // Claims are already validated by the authentication middleware, so any missing claim is a programming error
        if (!UserId.TryParse(httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
        {
            throw new InvalidOperationException("No 'sub' claim found in refresh token.");
        }

        if (!RefreshTokenId.TryParse(httpContext.User.FindFirstValue("rtid"), out var refreshTokenId))
        {
            throw new InvalidOperationException("No 'rtid' claim found in refresh token.");
        }

        if (!int.TryParse(httpContext.User.FindFirstValue("rtv"), out var refreshTokenVersion))
        {
            throw new InvalidOperationException("No 'rtv' claim found in refresh token.");
        }

        var jwtId = httpContext.User.FindFirstValue(JwtRegisteredClaimNames.Jti);
        if (jwtId is null) throw new InvalidOperationException("No 'jti' claim found in refresh token.");

        var expiresClaim = httpContext.User.FindFirstValue(JwtRegisteredClaimNames.Exp);
        if (expiresClaim is null) throw new InvalidOperationException("No 'exp' claim found in refresh token.");
        var refreshTokenExpires = DateTimeOffset.FromUnixTimeSeconds(long.Parse(expiresClaim)); // Convert the expiration time from seconds since Unix epoch

        var user = await userRepository.GetByIdAsync(userId, cancellationToken);
        if (user is null)
        {
            logger.LogWarning("No user found with user id {UserId} found.", userId);
            return Result.Unauthorized($"No user found with user id {userId} found.");
        }

        // TODO: Check if the refreshTokenId exists in the database and if the jwtId and refreshTokenVersion are valid

        authenticationTokenService.RefreshAuthenticationTokens(user, refreshTokenId, refreshTokenVersion, refreshTokenExpires);
        events.CollectEvent(new AuthenticationTokensRefreshed());

        return Result.Success();
    }
}
