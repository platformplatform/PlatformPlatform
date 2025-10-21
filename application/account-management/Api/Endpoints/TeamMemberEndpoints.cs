using PlatformPlatform.AccountManagement.Features.TeamMembers.Commands;
using PlatformPlatform.AccountManagement.Features.Teams.Domain;
using PlatformPlatform.SharedKernel.ApiResults;
using PlatformPlatform.SharedKernel.Endpoints;

namespace PlatformPlatform.AccountManagement.Api.Endpoints;

public sealed class TeamMemberEndpoints : IEndpoints
{
    private const string RoutesPrefix = "/api/account-management/teams";

    public void MapEndpoints(IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup(RoutesPrefix).WithTags("Team Members").RequireAuthorization().ProducesValidationProblem();

        group.MapPut("/{teamId}/members", async Task<ApiResult> (TeamId teamId, UpdateTeamMembersCommand command, IMediator mediator)
            => await mediator.Send(command with { TeamId = teamId })
        );
    }
}
