using Account.Features.Teams.Domain;
using Account.Features.Teams.Shared;
using Account.Features.Users.Domain;
using JetBrains.Annotations;
using SharedKernel.Cqrs;
using SharedKernel.Domain;
using SharedKernel.ExecutionContext;
using SharedKernel.Telemetry;

namespace Account.Features.Teams.Commands;

[PublicAPI]
public sealed record ChangeTeamMemberRoleCommand(UserId UserId, TeamMemberRole Role) : ICommand, IRequest<Result>
{
    [JsonIgnore] // Removes from API contract
    public TeamId TeamId { get; init; } = null!;
}

public sealed class ChangeTeamMemberRoleHandler(
    ITeamRepository teamRepository,
    ITeamMemberRepository teamMemberRepository,
    IExecutionContext executionContext,
    ITelemetryEventsCollector events
) : IRequestHandler<ChangeTeamMemberRoleCommand, Result>
{
    public async Task<Result> Handle(ChangeTeamMemberRoleCommand command, CancellationToken cancellationToken)
    {
        if (!executionContext.UserInfo.IsFeatureFlagEnabled(TeamsFeatureFlag.Key))
        {
            return Result.NotFound("Teams feature is not enabled for this tenant.");
        }

        if (executionContext.UserInfo.Role is not (nameof(UserRole.Owner) or nameof(UserRole.Admin)))
        {
            return Result.Forbidden("Only tenant owners and tenant admins can change team member roles.");
        }

        var team = await teamRepository.GetByIdAsync(command.TeamId, cancellationToken);
        if (team is null) return Result.NotFound($"Team with id '{command.TeamId}' not found.");

        var teamMember = await teamMemberRepository.GetByTeamAndUserIdAsync(command.TeamId, command.UserId, cancellationToken);
        if (teamMember is null)
        {
            return Result.NotFound($"User with id '{command.UserId}' is not a member of team '{command.TeamId}'.");
        }

        var previousRole = teamMember.Role;
        if (previousRole == command.Role) return Result.Success();

        teamMember.ChangeRole(command.Role);
        teamMemberRepository.Update(teamMember);

        events.CollectEvent(new TeamMemberRoleChanged(command.TeamId, command.UserId, previousRole, command.Role));

        return Result.Success();
    }
}
