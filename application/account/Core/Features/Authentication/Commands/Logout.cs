using JetBrains.Annotations;
using PlatformPlatform.Account.Features.Authentication.Domain;
using PlatformPlatform.SharedKernel.Authentication.TokenGeneration;
using PlatformPlatform.SharedKernel.Cqrs;
using PlatformPlatform.SharedKernel.ExecutionContext;
using PlatformPlatform.SharedKernel.Telemetry;

namespace PlatformPlatform.Account.Features.Authentication.Commands;

[PublicAPI]
public sealed record LogoutCommand : ICommand, IRequest<Result>;

public sealed class LogoutHandler(
    ISessionRepository sessionRepository,
    AuthenticationTokenService authenticationTokenService,
    IExecutionContext executionContext,
    TimeProvider timeProvider,
    ITelemetryEventsCollector events
) : IRequestHandler<LogoutCommand, Result>
{
    public async Task<Result> Handle(LogoutCommand command, CancellationToken cancellationToken)
    {
        var session = await sessionRepository.GetByIdAsync(executionContext.UserInfo.SessionId!, cancellationToken);
        if (session?.IsRevoked == false)
        {
            session.Revoke(timeProvider.GetUtcNow(), SessionRevokedReason.LoggedOut);
            sessionRepository.Update(session);
            events.CollectEvent(new SessionRevoked(SessionRevokedReason.LoggedOut));
        }

        authenticationTokenService.Logout();

        events.CollectEvent(new Logout());

        return Result.Success();
    }
}
