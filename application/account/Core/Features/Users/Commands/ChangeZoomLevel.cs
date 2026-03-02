using JetBrains.Annotations;
using SharedKernel.Cqrs;
using SharedKernel.Telemetry;

namespace Account.Features.Users.Commands;

[PublicAPI]
public sealed record ChangeZoomLevelCommand(string FromZoomLevel, string ZoomLevel) : ICommand, IRequest<Result>;

public sealed class ChangeZoomLevelHandler(ITelemetryEventsCollector events)
    : IRequestHandler<ChangeZoomLevelCommand, Result>
{
    public Task<Result> Handle(ChangeZoomLevelCommand command, CancellationToken cancellationToken)
    {
        events.CollectEvent(new UserZoomLevelChanged(command.FromZoomLevel, command.ZoomLevel));

        return Task.FromResult(Result.Success());
    }
}
