using System.Security.Claims;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Http;
using PlatformPlatform.SharedKernel.Authentication.TokenGeneration;
using PlatformPlatform.SharedKernel.Cqrs;
using PlatformPlatform.SharedKernel.Telemetry;

namespace PlatformPlatform.AccountManagement.Features.Authentication.Commands;

[PublicAPI]
public sealed record LogoutCommand : ICommand, IRequest<Result>;

public sealed class LogoutHandler(
    AuthenticationTokenService authenticationTokenService,
    IHttpContextAccessor httpContextAccessor,
    ITelemetryEventsCollector events,
    ILogger<LogoutHandler> logger
) : IRequestHandler<LogoutCommand, Result>
{
    public Task<Result> Handle(LogoutCommand command, CancellationToken cancellationToken)
    {
        var httpContext = httpContextAccessor.HttpContext ?? throw new InvalidOperationException("HttpContext is null.");

        var userIdentifier = httpContext.User.FindFirst(ClaimTypes.NameIdentifier);

        authenticationTokenService.Logout();

        if (userIdentifier is null)
        {
            logger.LogWarning("No user identifier found in claims.");
        }
        else
        {
            events.CollectEvent(new Logout());
        }

        return Task.FromResult(Result.Success());
    }
}
