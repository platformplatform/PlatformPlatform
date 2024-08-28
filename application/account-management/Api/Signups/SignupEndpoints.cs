using PlatformPlatform.AccountManagement.Core.Signups.Commands;
using PlatformPlatform.AccountManagement.Core.Signups.Domain;
using PlatformPlatform.AccountManagement.Core.Signups.Queries;
using PlatformPlatform.SharedKernel.ApiCore.ApiResults;
using PlatformPlatform.SharedKernel.ApiCore.Endpoints;

namespace PlatformPlatform.AccountManagement.Api.Signups;

public class SignupEndpoints : IEndpoints
{
    private const string RoutesPrefix = "/api/account-management/signups";

    public void MapEndpoints(IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup(RoutesPrefix).WithTags("Signups").RequireAuthorization();

        group.MapGet("/is-subdomain-free", async Task<ApiResult<bool>> ([AsParameters] IsSubdomainFreeQuery query, ISender mediator)
            => await mediator.Send(query)
        ).Produces<bool>().AllowAnonymous();

        group.MapPost("/start", async Task<ApiResult<StartSignupResponse>> (StartSignupCommand command, ISender mediator)
            => await mediator.Send(command)
        ).Produces<StartSignupResponse>().AllowAnonymous();

        group.MapPost("{id}/complete", async Task<ApiResult> (SignupId id, CompleteSignupCommand command, ISender mediator)
            => await mediator.Send(command with { Id = id })
        ).AllowAnonymous();
    }
}
