using JetBrains.Annotations;
using PlatformPlatform.AccountManagement.Features.TeamMembers.Domain;
using PlatformPlatform.AccountManagement.Features.Teams.Domain;
using PlatformPlatform.AccountManagement.Features.Users.Domain;
using PlatformPlatform.SharedKernel.Cqrs;
using PlatformPlatform.SharedKernel.Domain;
using PlatformPlatform.SharedKernel.ExecutionContext;
using PlatformPlatform.SharedKernel.Telemetry;

namespace PlatformPlatform.AccountManagement.Features.TeamMembers.Commands;

[PublicAPI]
public sealed record ChangeTeamMemberRoleCommand(TeamMemberRole Role) : ICommand, IRequest<Result>
{
    [JsonIgnore]
    public TeamId TeamId { get; init; } = null!;

    [JsonIgnore]
    public UserId UserId { get; init; } = null!;
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
        var isTeamAdmin = await teamMemberRepository.IsUserTeamAdminAsync(
            command.TeamId,
            executionContext.UserInfo.Id!,
            cancellationToken
        );

        if (!isTeamAdmin && executionContext.UserInfo.Role != nameof(UserRole.Owner))
        {
            return Result.Forbidden("Only team admins or tenant owners can change member roles.");
        }

        if (isTeamAdmin &&
            executionContext.UserInfo.Role != nameof(UserRole.Owner) &&
            command.UserId == executionContext.UserInfo.Id! &&
            command.Role != TeamMemberRole.Admin)
        {
            return Result.Forbidden("Team admins cannot demote themselves from admin role.");
        }

        var team = await teamRepository.GetByIdAsync(command.TeamId, cancellationToken);
        if (team is null)
        {
            return Result.NotFound($"Team with ID '{command.TeamId}' not found.");
        }

        var teamMember = await teamMemberRepository.GetByTeamAndUserAsync(command.TeamId, command.UserId, cancellationToken);
        if (teamMember is null)
        {
            return Result.NotFound($"Team member with user ID '{command.UserId}' not found in team '{command.TeamId}'.");
        }

        var oldRole = teamMember.Role;
        teamMember.ChangeRole(command.Role);
        teamMemberRepository.Update(teamMember);

        events.CollectEvent(new TeamMemberRoleChanged(teamMember.Id, teamMember.TeamId, teamMember.UserId, oldRole, command.Role));

        return Result.Success();
    }
}
