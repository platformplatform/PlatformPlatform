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
public sealed record UpdateTeamMembersCommand(UserId[] AddUserIds, UserId[] RemoveUserIds) : ICommand, IRequest<Result>
{
    [JsonIgnore] // Removes from API contract
    public TeamId TeamId { get; init; } = null!;
}

public sealed class UpdateTeamMembersHandler(
    ITeamRepository teamRepository,
    ITeamMemberRepository teamMemberRepository,
    IUserRepository userRepository,
    IExecutionContext executionContext,
    ITelemetryEventsCollector events
) : IRequestHandler<UpdateTeamMembersCommand, Result>
{
    public async Task<Result> Handle(UpdateTeamMembersCommand command, CancellationToken cancellationToken)
    {
        if (!executionContext.UserInfo.IsFeatureFlagEnabled(TeamsFeatureFlag.Key))
        {
            return Result.NotFound("Teams feature is not enabled for this tenant.");
        }

        var team = await teamRepository.GetByIdAsync(command.TeamId, cancellationToken);
        if (team is null) return Result.NotFound($"Team with id '{command.TeamId}' not found.");

        var isTenantOwnerOrAdmin = executionContext.UserInfo.Role is nameof(UserRole.Owner) or nameof(UserRole.Admin);
        var isTeamAdmin = executionContext.UserInfo.Id is not null
                          && await teamMemberRepository.IsUserAdminOfTeamAsync(command.TeamId, executionContext.UserInfo.Id, cancellationToken);

        if (!isTenantOwnerOrAdmin && !isTeamAdmin)
        {
            return Result.Forbidden("Only team admins, tenant owners, or tenant admins can manage team members.");
        }

        var addUserIds = command.AddUserIds.Distinct().ToArray();
        var removeUserIds = command.RemoveUserIds.Distinct().ToArray();

        var existingMembers = await teamMemberRepository.GetByTeamIdAsync(command.TeamId, cancellationToken);
        var existingUserIds = existingMembers.Select(m => m.UserId).ToHashSet();

        if (addUserIds.Length > 0)
        {
            var usersToAdd = await userRepository.GetByIdsAsync(addUserIds, cancellationToken);
            var validUserIds = usersToAdd.Select(u => u.Id).ToHashSet();

            foreach (var userId in addUserIds)
            {
                if (!validUserIds.Contains(userId))
                {
                    return Result.NotFound($"User with id '{userId}' not found.");
                }

                if (existingUserIds.Contains(userId)) continue;

                var teamMember = TeamMember.Create(executionContext.TenantId!, command.TeamId, userId);
                await teamMemberRepository.AddAsync(teamMember, cancellationToken);
                events.CollectEvent(new TeamMemberAdded(command.TeamId, userId));
            }
        }

        if (removeUserIds.Length > 0)
        {
            var membersByUserId = existingMembers.ToDictionary(m => m.UserId);
            foreach (var userId in removeUserIds)
            {
                if (!membersByUserId.TryGetValue(userId, out var member)) continue;

                teamMemberRepository.Remove(member);
                events.CollectEvent(new TeamMemberRemoved(command.TeamId, userId));
            }
        }

        return Result.Success();
    }
}
