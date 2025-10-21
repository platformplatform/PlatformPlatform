using FluentValidation;
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
public sealed record MemberToAdd(UserId UserId, TeamMemberRole Role);

[PublicAPI]
public sealed record UpdateTeamMembersCommand(MemberToAdd[] MembersToAdd, UserId[] MemberIdsToRemove) : ICommand, IRequest<Result>
{
    [JsonIgnore] // Removes from API contract
    public TeamId TeamId { get; init; } = null!;
}

public sealed class UpdateTeamMembersValidator : AbstractValidator<UpdateTeamMembersCommand>
{
    public UpdateTeamMembersValidator()
    {
        RuleFor(x => x.MembersToAdd.Length + x.MemberIdsToRemove.Length)
            .LessThanOrEqualTo(100)
            .WithMessage("Total operations (adds + removes) must not exceed 100.");
    }
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
        // Check authorization: user is either Team Admin or Tenant Owner
        var isTeamAdmin = await teamMemberRepository.IsUserTeamAdminAsync(
            command.TeamId,
            executionContext.UserInfo.Id!,
            cancellationToken
        );

        if (!isTeamAdmin && executionContext.UserInfo.Role != nameof(UserRole.Owner))
        {
            return Result.Forbidden("Only team admins or tenant owners can update team members.");
        }

        // Prevent Team Admin from removing themselves
        if (isTeamAdmin &&
            executionContext.UserInfo.Role != nameof(UserRole.Owner) &&
            command.MemberIdsToRemove.Contains(executionContext.UserInfo.Id!))
        {
            return Result.Forbidden("Team admins cannot remove themselves from the team.");
        }

        // Get team by ID and return NotFound if not exists
        var team = await teamRepository.GetByIdAsync(command.TeamId, cancellationToken);
        if (team is null)
        {
            return Result.NotFound($"Team with ID '{command.TeamId}' not found.");
        }

        var addedMembers = new List<TeamMember>();
        var removedMembers = new List<TeamMember>();

        // Process members to add
        if (command.MembersToAdd.Length > 0)
        {
            // Get users being added
            var userIdsToAdd = command.MembersToAdd.Select(m => m.UserId).ToArray();
            var usersToAdd = await userRepository.GetByIdsAsync(userIdsToAdd, cancellationToken);

            if (usersToAdd.Length != userIdsToAdd.Length)
            {
                var missingUserIds = userIdsToAdd.Except(usersToAdd.Select(u => u.Id));
                return Result.NotFound($"Users not found: {string.Join(", ", missingUserIds)}.");
            }

            // Verify all users belong to same tenant as team
            if (usersToAdd.Any(u => u.TenantId != team.TenantId))
            {
                return Result.Forbidden("Cannot add users from different tenant to team.");
            }

            // Get existing team members to check for duplicates
            var existingMembers = await teamMemberRepository.GetByTeamIdAsync(command.TeamId, cancellationToken);
            var existingUserIds = existingMembers.Select(m => m.UserId).ToHashSet();

            // Filter out users who are already members (silently skip duplicates)
            var newMembers = command.MembersToAdd
                .Where(m => !existingUserIds.Contains(m.UserId))
                .Select(m => TeamMember.Create(team.TenantId, command.TeamId, m.UserId, m.Role))
                .ToArray();

            if (newMembers.Length > 0)
            {
                await teamMemberRepository.BulkAddAsync(newMembers, cancellationToken);
                addedMembers.AddRange(newMembers);

                // Collect individual telemetry events for each added member
                foreach (var member in newMembers)
                {
                    events.CollectEvent(new TeamMemberAdded(member.Id, member.TeamId, member.UserId, member.Role));
                }
            }
        }

        // Process members to remove
        if (command.MemberIdsToRemove.Length > 0)
        {
            var allTeamMembers = await teamMemberRepository.GetByTeamIdAsync(command.TeamId, cancellationToken);
            var membersToRemove = allTeamMembers.Where(m => command.MemberIdsToRemove.Contains(m.UserId)).ToArray();

            if (membersToRemove.Length != command.MemberIdsToRemove.Length)
            {
                var missingUserIds = command.MemberIdsToRemove.Except(membersToRemove.Select(m => m.UserId));
                return Result.NotFound($"Team members not found for users: {string.Join(", ", missingUserIds)}.");
            }

            teamMemberRepository.BulkRemove(membersToRemove);
            removedMembers.AddRange(membersToRemove);

            // Collect individual telemetry events for each removed member
            foreach (var member in membersToRemove)
            {
                events.CollectEvent(new TeamMemberRemoved(member.Id, member.TeamId, member.UserId));
            }
        }

        // Collect aggregate telemetry event
        events.CollectEvent(new TeamMembersUpdated(command.TeamId, addedMembers.Count, removedMembers.Count));

        return Result.Success();
    }
}
