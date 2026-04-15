using Account.Features.Teams.Domain;
using Account.Features.Teams.Shared;
using JetBrains.Annotations;
using SharedKernel.Cqrs;
using SharedKernel.ExecutionContext;

namespace Account.Features.Teams.Queries;

[PublicAPI]
public sealed record GetTeamQuery : IRequest<Result<TeamResponse>>
{
    [JsonIgnore] // Removes from API contract
    public TeamId Id { get; init; } = null!;
}

public sealed class GetTeamHandler(ITeamRepository teamRepository, IExecutionContext executionContext)
    : IRequestHandler<GetTeamQuery, Result<TeamResponse>>
{
    public async Task<Result<TeamResponse>> Handle(GetTeamQuery query, CancellationToken cancellationToken)
    {
        if (!executionContext.UserInfo.IsFeatureFlagEnabled(TeamsFeatureFlag.Key))
        {
            return Result<TeamResponse>.NotFound("Teams feature is not enabled for this tenant.");
        }

        var team = await teamRepository.GetByIdAsync(query.Id, cancellationToken);
        if (team is null) return Result<TeamResponse>.NotFound($"Team with id '{query.Id}' not found.");

        return TeamResponse.FromTeam(team);
    }
}
