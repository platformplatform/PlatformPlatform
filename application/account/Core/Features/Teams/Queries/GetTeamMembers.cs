using Account.Features.Teams.Domain;
using Account.Features.Users.Domain;
using JetBrains.Annotations;
using SharedKernel.Cqrs;
using SharedKernel.Domain;
using SharedKernel.ExecutionContext;

namespace Account.Features.Teams.Queries;

[PublicAPI]
public sealed record GetTeamMembersQuery : IRequest<Result<TeamMembersResponse>>
{
    [JsonIgnore] // Removes from API contract
    public TeamId TeamId { get; init; } = null!;
}

[PublicAPI]
public sealed record TeamMembersResponse(TeamMemberDetails[] Members);

[PublicAPI]
public sealed record TeamMemberDetails(
    UserId UserId,
    string Email,
    string? FirstName,
    string? LastName,
    string? Title,
    string? AvatarUrl,
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
        if (team is null) return Result<TeamMembersResponse>.NotFound($"Team with id '{query.TeamId}' not found.");

        var isTenantOwnerOrAdmin = executionContext.UserInfo.Role is nameof(UserRole.Owner) or nameof(UserRole.Admin);
        var isTeamMember = executionContext.UserInfo.Id is not null
                           && await teamMemberRepository.IsUserMemberOfTeamAsync(query.TeamId, executionContext.UserInfo.Id, cancellationToken);

        if (!isTenantOwnerOrAdmin && !isTeamMember)
        {
            return Result<TeamMembersResponse>.Forbidden("Only team members, tenant owners, or tenant admins can view team members.");
        }

        var teamMembers = await teamMemberRepository.GetByTeamIdAsync(query.TeamId, cancellationToken);
        var userIds = teamMembers.Select(tm => tm.UserId).ToArray();
        var users = await userRepository.GetByIdsAsync(userIds, cancellationToken);
        var usersById = users.ToDictionary(u => u.Id);

        var memberDetails = teamMembers
            .Where(tm => usersById.ContainsKey(tm.UserId))
            .Select(tm =>
                {
                    var user = usersById[tm.UserId];
                    return new TeamMemberDetails(
                        user.Id,
                        user.Email,
                        user.FirstName,
                        user.LastName,
                        user.Title,
                        user.Avatar.Url,
                        tm.Role
                    );
                }
            )
            .OrderBy(m => m.FirstName ?? "")
            .ThenBy(m => m.LastName ?? "")
            .ThenBy(m => m.Email)
            .ToArray();

        return new TeamMembersResponse(memberDetails);
    }
}
