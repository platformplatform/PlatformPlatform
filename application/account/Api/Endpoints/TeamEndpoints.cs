using Account.Features.Teams.Commands;
using Account.Features.Teams.Domain;
using Account.Features.Teams.Queries;
using Account.Features.Teams.Shared;
using SharedKernel.ApiResults;
using SharedKernel.Endpoints;

namespace Account.Api.Endpoints;

public sealed class TeamEndpoints : IEndpoints
{
    private const string RoutesPrefix = "/api/account/teams";

    public void MapEndpoints(IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup(RoutesPrefix).WithTags("Teams").RequireAuthorization().ProducesValidationProblem();

        group.MapGet("/", async Task<ApiResult<GetTeamsResponse>> ([AsParameters] GetTeamsQuery query, IMediator mediator)
            => await mediator.Send(query)
        ).Produces<GetTeamsResponse>();

        group.MapGet("/{id}", async Task<ApiResult<TeamResponse>> (TeamId id, IMediator mediator)
            => await mediator.Send(new GetTeamQuery { Id = id })
        ).Produces<TeamResponse>();

        group.MapPost("/", async Task<ApiResult<TeamResponse>> (CreateTeamCommand command, IMediator mediator)
            => await mediator.Send(command)
        ).Produces<TeamResponse>();

        group.MapPut("/{id}", async Task<ApiResult<TeamResponse>> (TeamId id, UpdateTeamCommand command, IMediator mediator)
            => await mediator.Send(command with { Id = id })
        ).Produces<TeamResponse>();

        group.MapDelete("/{id}", async Task<ApiResult> (TeamId id, IMediator mediator)
            => await mediator.Send(new DeleteTeamCommand(id))
        );
    }
}
