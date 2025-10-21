using JetBrains.Annotations;
using PlatformPlatform.AccountManagement.Features.TeamMembers.Domain;
using PlatformPlatform.AccountManagement.Features.Teams.Domain;
using PlatformPlatform.AccountManagement.Features.Users.Domain;
using PlatformPlatform.SharedKernel.Cqrs;
using PlatformPlatform.SharedKernel.Domain;
using PlatformPlatform.SharedKernel.ExecutionContext;

namespace PlatformPlatform.AccountManagement.Features.TeamMembers.Queries;

[PublicAPI]
public sealed record GetTeamMembersQuery(TeamId TeamId) : IRequest<Result<TeamMembersResponse>>;

[PublicAPI]
public sealed record TeamMembersResponse(TeamMemberDetails[] Members);

[PublicAPI]
public sealed record TeamMemberDetails(
    TeamMemberId TeamMemberId,
    UserId UserId,
    string UserName,
    string UserEmail,
    string UserTitle,
    Avatar UserAvatar,
    TeamMemberRole Role
);

public sealed class GetTeamMembersHandler(
    ITeamRepository teamRepository,
    ITeamMemberRepository teamMemberRepository,
    IUserRepository userRepository,
    IExecutionContext executionContext
) : IRequestHandler<GetTeamMembersQuery, Result<TeamMembersResponse>>
{
    public async Task<Result<TeamMembersResponse>> Handle(GetTeamMembersQuery query, CancellationToken cancellationToken)
    {
        var team = await teamRepository.GetByIdAsync(query.TeamId, cancellationToken);
        if (team is null)
        {
            return Result<TeamMembersResponse>.NotFound($"Team with ID '{query.TeamId}' not found.");
        }

        var isTeamAdmin = await teamMemberRepository.IsUserTeamAdminAsync(
            query.TeamId,
            executionContext.UserInfo.Id!,
            cancellationToken
        );

        var teamMember = await teamMemberRepository.GetByTeamAndUserAsync(
            query.TeamId,
            executionContext.UserInfo.Id!,
            cancellationToken
        );

        var isTenantOwner = executionContext.UserInfo.Role == nameof(UserRole.Owner);

        if (!isTeamAdmin && teamMember is null && !isTenantOwner)
        {
            return Result<TeamMembersResponse>.Forbidden("Only team members or tenant owners can view team members.");
        }

        var teamMembers = await teamMemberRepository.GetByTeamIdAsync(query.TeamId, cancellationToken);

        if (teamMembers.Length == 0)
        {
            return new TeamMembersResponse([]);
        }

        var userIds = teamMembers.Select(m => m.UserId).ToArray();
        var users = await userRepository.GetByIdsAsync(userIds, cancellationToken);
        var userMap = users.ToDictionary(u => u.Id);

        var memberDetails = teamMembers
            .Where(m => userMap.TryGetValue(m.UserId, out _))
            .OrderByDescending(m => m.Role == TeamMemberRole.Admin)
            .ThenBy(m => userMap[m.UserId].FirstName)
            .ThenBy(m => userMap[m.UserId].LastName)
            .Select(m =>
                {
                    var user = userMap[m.UserId];
                    return new TeamMemberDetails(
                        m.Id,
                        m.UserId,
                        $"{user.FirstName} {user.LastName}".Trim(),
                        user.Email,
                        user.Title ?? "",
                        user.Avatar,
                        m.Role
                    );
                }
            )
            .ToArray();

        return new TeamMembersResponse(memberDetails);
    }
}
