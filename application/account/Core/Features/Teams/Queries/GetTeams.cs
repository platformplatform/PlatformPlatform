using Account.Features.Teams.Domain;
using Account.Features.Teams.Shared;
using JetBrains.Annotations;
using SharedKernel.Cqrs;

namespace Account.Features.Teams.Queries;

[PublicAPI]
public sealed record GetTeamsQuery : IRequest<Result<GetTeamsResponse>>;

[PublicAPI]
public sealed record GetTeamsResponse(TeamResponse[] Teams);

public sealed class GetTeamsHandler(ITeamRepository teamRepository)
    : IRequestHandler<GetTeamsQuery, Result<GetTeamsResponse>>
{
    public async Task<Result<GetTeamsResponse>> Handle(GetTeamsQuery query, CancellationToken cancellationToken)
    {
        var teams = await teamRepository.GetAllAsync(cancellationToken);
        var teamResponses = teams.Select(TeamResponse.FromTeam).ToArray();
        return new GetTeamsResponse(teamResponses);
    }
}
