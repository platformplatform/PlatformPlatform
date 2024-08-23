using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using PlatformPlatform.AccountManagement.Application.TelemetryEvents;
using PlatformPlatform.SharedKernel.ApplicationCore.Cqrs;
using PlatformPlatform.SharedKernel.ApplicationCore.TelemetryEvents;

namespace PlatformPlatform.AccountManagement.Application.Authentication;

public sealed record LogoutCommand
    : ICommand, IRequest<Result>;

public sealed class LogutHandler(
    AuthenticationTokenService authenticationTokenService,
    IHttpContextAccessor httpContextAccessor,
    ITelemetryEventsCollector events,
    ILogger<CompleteLoginHandler> logger
) : IRequestHandler<LogoutCommand, Result>
{
    public Task<Result> Handle(LogoutCommand command, CancellationToken cancellationToken)
    {
        var httpContext = httpContextAccessor.HttpContext ?? throw new InvalidOperationException("HttpContext is null.");

        var userIdentifier = httpContext.User.FindFirst(ClaimTypes.NameIdentifier);

        authenticationTokenService.Logout();

        if (userIdentifier is null || !UserId.TryParse(userIdentifier.Value, out var userId))
        {
            logger.LogWarning("No user identifier found in claims.");
        }
        else
        {
            events.CollectEvent(new Logout(userId!));
        }

        return Task.FromResult(Result.Success());
    }
}
