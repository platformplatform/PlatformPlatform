using PlatformPlatform.AccountManagement.Signups.Commands;
using PlatformPlatform.AccountManagement.Signups.Domain;
using PlatformPlatform.AccountManagement.Signups.Queries;
using PlatformPlatform.SharedKernel.ApiResults;
using PlatformPlatform.SharedKernel.Endpoints;

namespace PlatformPlatform.AccountManagement.Api.Endpoints;

public sealed class SignupEndpoints : IEndpoints
{
    private const string RoutesPrefix = "/api/account-management/signups";

    public void MapEndpoints(IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup(RoutesPrefix).WithTags("Signups").RequireAuthorization();

        group.MapGet("/is-subdomain-free", async Task<ApiResult<bool>> ([AsParameters] IsSubdomainFreeQuery query, IMediator mediator)
            => await mediator.Send(query)
        ).Produces<bool>().AllowAnonymous();

        group.MapPost("/start", async Task<ApiResult<StartSignupResponse>> (StartSignupCommand command, IMediator mediator)
            => await mediator.Send(command)
        ).Produces<StartSignupResponse>().AllowAnonymous();

        group.MapPost("{id}/complete", async Task<ApiResult> (SignupId id, CompleteSignupCommand command, IMediator mediator)
            => await mediator.Send(command with { Id = id })
        ).AllowAnonymous();
    }
}
