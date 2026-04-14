using Account.Features.Teams.Domain;
using JetBrains.Annotations;

namespace Account.Features.Teams.Shared;

[PublicAPI]
public sealed record TeamResponse(
    TeamId Id,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ModifiedAt,
    string Name,
    string? Description
)
{
    public static TeamResponse FromTeam(Team team)
    {
        return new TeamResponse(team.Id, team.CreatedAt, team.ModifiedAt, team.Name, team.Description);
    }
}
