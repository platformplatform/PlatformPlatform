using PlatformPlatform.AccountManagement.Features.Teams.Commands;
using PlatformPlatform.AccountManagement.Features.Teams.Domain;
using PlatformPlatform.AccountManagement.Features.Teams.Queries;
using PlatformPlatform.SharedKernel.ApiResults;
using PlatformPlatform.SharedKernel.Endpoints;

namespace PlatformPlatform.AccountManagement.Api.Endpoints;

public sealed class TeamEndpoints : IEndpoints
{
    private const string RoutesPrefix = "/api/account-management/teams";

    public void MapEndpoints(IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup(RoutesPrefix).WithTags("Teams").RequireAuthorization().ProducesValidationProblem();

        group.MapPost("/", async Task<ApiResult<TeamId>> (CreateTeamCommand command, IMediator mediator)
            => await mediator.Send(command)
        ).Produces<TeamId>(StatusCodes.Status201Created);

        group.MapGet("/{id}", async Task<ApiResult<TeamResponse>> (TeamId id, IMediator mediator)
            => await mediator.Send(new GetTeamQuery(id))
        ).Produces<TeamResponse>();
    }
}
