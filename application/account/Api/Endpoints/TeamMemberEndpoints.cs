using Account.Features.Teams.Commands;
using Account.Features.Teams.Domain;
using Account.Features.Teams.Queries;
using SharedKernel.ApiResults;
using SharedKernel.Domain;
using SharedKernel.Endpoints;

namespace Account.Api.Endpoints;

public sealed class TeamMemberEndpoints : IEndpoints
{
    private const string RoutesPrefix = "/api/account/teams/{teamId}/members";

    public void MapEndpoints(IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup(RoutesPrefix).WithTags("TeamMembers").RequireAuthorization().ProducesValidationProblem();

        group.MapGet("/", async Task<ApiResult<TeamMembersResponse>> (TeamId teamId, IMediator mediator)
            => await mediator.Send(new GetTeamMembersQuery { TeamId = teamId })
        ).Produces<TeamMembersResponse>();

        group.MapPut("/", async Task<ApiResult> (TeamId teamId, UpdateTeamMembersCommand command, IMediator mediator)
            => await mediator.Send(command with { TeamId = teamId })
        );

        group.MapPut("/{userId}/role", async Task<ApiResult> (TeamId teamId, UserId userId, ChangeTeamMemberRoleCommand command, IMediator mediator)
            => await mediator.Send(command with { TeamId = teamId, UserId = userId })
        );
    }
}
