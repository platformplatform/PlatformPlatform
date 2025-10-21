using JetBrains.Annotations;
using PlatformPlatform.AccountManagement.Features.Teams.Domain;
using PlatformPlatform.AccountManagement.Features.Users.Domain;
using PlatformPlatform.SharedKernel.Cqrs;
using PlatformPlatform.SharedKernel.ExecutionContext;
using PlatformPlatform.SharedKernel.Telemetry;

namespace PlatformPlatform.AccountManagement.Features.Teams.Commands;

[PublicAPI]
public sealed record DeleteTeamCommand(TeamId Id) : ICommand, IRequest<Result>;

public sealed class DeleteTeamHandler(
    ITeamRepository teamRepository,
    IExecutionContext executionContext,
    ITelemetryEventsCollector events
) : IRequestHandler<DeleteTeamCommand, Result>
{
    public async Task<Result> Handle(DeleteTeamCommand command, CancellationToken cancellationToken)
    {
        if (executionContext.UserInfo.Role != nameof(UserRole.Owner))
        {
            return Result.Forbidden("Only tenant owners can delete teams.");
        }

        var team = await teamRepository.GetByIdAsync(command.Id, cancellationToken);

        if (team is null)
        {
            return Result.NotFound($"Team with ID '{command.Id}' not found.");
        }

        teamRepository.Remove(team);

        events.CollectEvent(new TeamDeleted(team.Id));

        return Result.Success();
    }
}
