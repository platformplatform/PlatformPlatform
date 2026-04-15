using Account.Features.Teams.Domain;
using Account.Features.Teams.Shared;
using JetBrains.Annotations;
using SharedKernel.Cqrs;
using SharedKernel.ExecutionContext;

namespace Account.Features.Teams.Queries;

[PublicAPI]
public sealed record GetTeamsQuery : IRequest<Result<GetTeamsResponse>>;

[PublicAPI]
public sealed record GetTeamsResponse(TeamResponse[] Teams);

public sealed class GetTeamsHandler(ITeamRepository teamRepository, IExecutionContext executionContext)
    : IRequestHandler<GetTeamsQuery, Result<GetTeamsResponse>>
{
    public async Task<Result<GetTeamsResponse>> Handle(GetTeamsQuery query, CancellationToken cancellationToken)
    {
        if (!executionContext.UserInfo.IsFeatureFlagEnabled(TeamsFeatureFlag.Key))
        {
            return Result<GetTeamsResponse>.NotFound("Teams feature is not enabled for this tenant.");
        }

        var teams = await teamRepository.GetAllAsync(cancellationToken);
        var teamResponses = teams.Select(TeamResponse.FromTeam).ToArray();
        return new GetTeamsResponse(teamResponses);
    }
}
