using JetBrains.Annotations;
using PlatformPlatform.SharedKernel.Authentication.TokenGeneration;
using PlatformPlatform.SharedKernel.Cqrs;
using PlatformPlatform.SharedKernel.Telemetry;

namespace PlatformPlatform.AccountManagement.Features.Authentication.Commands;

[PublicAPI]
public sealed record LogoutCommand : ICommand, IRequest<Result>;

public sealed class LogoutHandler(AuthenticationTokenService authenticationTokenService, ITelemetryEventsCollector events)
    : IRequestHandler<LogoutCommand, Result>
{
    public Task<Result> Handle(LogoutCommand command, CancellationToken cancellationToken)
    {
        authenticationTokenService.Logout();

        events.CollectEvent(new Logout());

        return Task.FromResult(Result.Success());
    }
}
