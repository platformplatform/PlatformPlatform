using JetBrains.Annotations;
using Mapster;
using PlatformPlatform.AccountManagement.Features.Teams.Domain;
using PlatformPlatform.SharedKernel.Cqrs;
using PlatformPlatform.SharedKernel.Domain;

namespace PlatformPlatform.AccountManagement.Features.Teams.Queries;

[PublicAPI]
public sealed record GetTeamQuery(TeamId Id) : IRequest<Result<TeamResponse>>;

[PublicAPI]
public sealed record TeamResponse(
    TeamId Id,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ModifiedAt,
    string Name,
    string Description,
    TenantId TenantId
);

public sealed class GetTeamHandler(ITeamRepository teamRepository)
    : IRequestHandler<GetTeamQuery, Result<TeamResponse>>
{
    public async Task<Result<TeamResponse>> Handle(GetTeamQuery query, CancellationToken cancellationToken)
    {
        var team = await teamRepository.GetByIdAsync(query.Id, cancellationToken);

        if (team is null)
        {
            return Result<TeamResponse>.NotFound($"Team with ID '{query.Id}' not found.");
        }

        return team.Adapt<TeamResponse>();
    }
}
