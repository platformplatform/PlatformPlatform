using JetBrains.Annotations;
using PlatformPlatform.SharedKernel.Cqrs;
using PlatformPlatform.SharedKernel.Telemetry;

namespace PlatformPlatform.Account.Features.Users.Commands;

[PublicAPI]
public sealed record ChangeThemeCommand(string FromTheme, string Theme, string? ResolvedTheme) : ICommand, IRequest<Result>;

public sealed class ChangeThemeHandler(ITelemetryEventsCollector events)
    : IRequestHandler<ChangeThemeCommand, Result>
{
    public Task<Result> Handle(ChangeThemeCommand command, CancellationToken cancellationToken)
    {
        events.CollectEvent(new UserThemeChanged(command.FromTheme, command.Theme, command.ResolvedTheme));

        return Task.FromResult(Result.Success());
    }
}
