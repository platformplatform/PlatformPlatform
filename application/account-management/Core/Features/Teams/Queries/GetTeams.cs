using JetBrains.Annotations;
using PlatformPlatform.AccountManagement.Features.TeamMembers.Domain;
using PlatformPlatform.AccountManagement.Features.Teams.Domain;
using PlatformPlatform.SharedKernel.Cqrs;

namespace PlatformPlatform.AccountManagement.Features.Teams.Queries;

[PublicAPI]
public sealed record GetTeamsQuery : IRequest<Result<TeamsResponse>>;

[PublicAPI]
public sealed record TeamsResponse(TeamSummary[] Teams);

[PublicAPI]
public sealed record TeamSummary(
    TeamId Id,
    string Name,
    string Description,
    int MemberCount
);

public sealed class GetTeamsHandler(
    ITeamRepository teamRepository,
    ITeamMemberRepository teamMemberRepository
) : IRequestHandler<GetTeamsQuery, Result<TeamsResponse>>
{
    public async Task<Result<TeamsResponse>> Handle(GetTeamsQuery query, CancellationToken cancellationToken)
    {
        var teams = await teamRepository.GetAllAsync(cancellationToken);
        var allTeamMembers = await teamMemberRepository.GetAllAsync(cancellationToken);

        var memberCountByTeam = allTeamMembers
            .GroupBy(m => m.TeamId)
            .ToDictionary(g => g.Key, g => g.Count());

        var teamSummaries = teams
            .OrderBy(t => t.Name)
            .Select(t => new TeamSummary(
                    t.Id,
                    t.Name,
                    t.Description,
                    memberCountByTeam.TryGetValue(t.Id, out var count) ? count : 0
                )
            )
            .ToArray();

        return new TeamsResponse(teamSummaries);
    }
}
