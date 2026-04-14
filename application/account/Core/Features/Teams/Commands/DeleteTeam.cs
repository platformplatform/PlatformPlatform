using Account.Features.Teams.Domain;
using Account.Features.Users.Domain;
using JetBrains.Annotations;
using SharedKernel.Cqrs;
using SharedKernel.ExecutionContext;
using SharedKernel.Telemetry;

namespace Account.Features.Teams.Commands;

[PublicAPI]
public sealed record DeleteTeamCommand(TeamId Id) : ICommand, IRequest<Result>;

public sealed class DeleteTeamHandler(ITeamRepository teamRepository, IExecutionContext executionContext, ITelemetryEventsCollector events)
    : IRequestHandler<DeleteTeamCommand, Result>
{
    public async Task<Result> Handle(DeleteTeamCommand command, CancellationToken cancellationToken)
    {
        if (executionContext.UserInfo.Role is not (nameof(UserRole.Owner) or nameof(UserRole.Admin)))
        {
            return Result.Forbidden("Only owners and admins can delete teams.");
        }

        var team = await teamRepository.GetByIdAsync(command.Id, cancellationToken);
        if (team is null) return Result.NotFound($"Team with id '{command.Id}' not found.");

        teamRepository.Remove(team);

        events.CollectEvent(new TeamDeleted(team.Id));

        return Result.Success();
    }
}
