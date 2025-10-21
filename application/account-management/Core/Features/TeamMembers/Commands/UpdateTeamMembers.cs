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
        var isTeamAdmin = await teamMemberRepository.IsUserTeamAdminAsync(
            command.TeamId,
            executionContext.UserInfo.Id!,
            cancellationToken
        );

        if (!isTeamAdmin && executionContext.UserInfo.Role != nameof(UserRole.Owner))
        {
            return Result.Forbidden("Only team admins or tenant owners can update team members.");
        }

        if (isTeamAdmin &&
            executionContext.UserInfo.Role != nameof(UserRole.Owner) &&
            command.MemberIdsToRemove.Contains(executionContext.UserInfo.Id!))
        {
            return Result.Forbidden("Team admins cannot remove themselves from the team.");
        }

        var team = await teamRepository.GetByIdAsync(command.TeamId, cancellationToken);
        if (team is null)
        {
            return Result.NotFound($"Team with ID '{command.TeamId}' not found.");
        }

        var addedMembers = new List<TeamMember>();
        var removedMembers = new List<TeamMember>();

        if (command.MembersToAdd.Length > 0)
        {
            var userIdsToAdd = command.MembersToAdd.Select(m => m.UserId).ToArray();

            // Use unfiltered query to detect cross-tenant violations before tenant filter hides them
            var usersToAddUnfiltered = await userRepository.GetByIdsUnfilteredAsync(userIdsToAdd, cancellationToken);

            if (usersToAddUnfiltered.Any(u => u.TenantId != team.TenantId))
            {
                return Result.Forbidden("Cannot add users from different tenant to team.");
            }

            var usersToAdd = await userRepository.GetByIdsAsync(userIdsToAdd, cancellationToken);

            if (usersToAdd.Length != userIdsToAdd.Length)
            {
                var missingUserIds = userIdsToAdd.Except(usersToAdd.Select(u => u.Id));
                return Result.NotFound($"Users not found: {string.Join(", ", missingUserIds)}.");
            }

            var existingMembers = await teamMemberRepository.GetByTeamIdAsync(command.TeamId, cancellationToken);
            var existingUserIds = existingMembers.Select(m => m.UserId).ToHashSet();

            var newMembers = command.MembersToAdd
                .Where(m => !existingUserIds.Contains(m.UserId))
                .Select(m => TeamMember.Create(team.TenantId, command.TeamId, m.UserId, m.Role))
                .ToArray();

            if (newMembers.Length > 0)
            {
                await teamMemberRepository.BulkAddAsync(newMembers, cancellationToken);
                addedMembers.AddRange(newMembers);

                foreach (var member in newMembers)
                {
                    events.CollectEvent(new TeamMemberAdded(member.Id, member.TeamId, member.UserId, member.Role));
                }
            }
        }

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

            foreach (var member in membersToRemove)
            {
                events.CollectEvent(new TeamMemberRemoved(member.Id, member.TeamId, member.UserId));
            }
        }

        events.CollectEvent(new TeamMembersUpdated(command.TeamId, addedMembers.Count, removedMembers.Count));

        return Result.Success();
    }
}
