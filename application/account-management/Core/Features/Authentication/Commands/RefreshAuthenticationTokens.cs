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

        UserId.TryParse(httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId);
        if (userId is null) throw new InvalidOperationException("No 'sub' claim found in refresh token.");

        var refreshTokenId = httpContext.User.FindFirstValue(JwtRegisteredClaimNames.Jti);
        if (refreshTokenId is null) throw new InvalidOperationException("No 'jti' claim found in refresh token.");

        var refreshChainTokenId = httpContext.User.FindFirstValue("rtid");
        if (refreshChainTokenId is null) throw new InvalidOperationException("No 'rtid' claim found in refresh token.");

        var hasValidRefreshTokenVersion = int.TryParse(httpContext.User.FindFirstValue("rtv"), out var refreshTokenVersion);
        if (!hasValidRefreshTokenVersion) throw new InvalidOperationException("No 'rtv' claim found in refresh token.");

        var expiresClaim = httpContext.User.FindFirstValue(JwtRegisteredClaimNames.Exp);
        if (expiresClaim is null) throw new InvalidOperationException("No 'exp' claim found in refresh token.");
        var refreshTokenExpires = DateTimeOffset.FromUnixTimeSeconds(long.Parse(expiresClaim)); // Convert the expiration time from seconds since Unix epoch

        var user = await userRepository.GetByIdAsync(userId, cancellationToken);
        if (user is null)
        {
            logger.LogWarning("No user found with user id {UserId} found.", userId);
            return Result.Unauthorized($"No user found with user id {userId} found.");
        }

        // TODO: Check if the refreshChainTokenId exists in the database and if the refreshTokenId and version are valid

        authenticationTokenService.RefreshAuthenticationTokens(user, refreshChainTokenId, refreshTokenVersion, refreshTokenExpires);
        events.CollectEvent(new AuthenticationTokensRefreshed(user.Id));

        return Result.Success();
    }
}
