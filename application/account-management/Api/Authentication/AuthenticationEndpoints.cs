using PlatformPlatform.AccountManagement.Application.Authentication;
using PlatformPlatform.AccountManagement.Domain.Authentication;
using PlatformPlatform.SharedKernel.ApiCore.ApiResults;
using PlatformPlatform.SharedKernel.ApiCore.Endpoints;

namespace PlatformPlatform.AccountManagement.Api.Authentication;

public class AuthenticationEndpoints : IEndpoints
{
    private const string RoutesPrefix = "/api/account-management/authentication";

    public void MapEndpoints(IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup(RoutesPrefix).WithTags("Authentication").AllowAnonymous();

        group.MapPost("/login/start", async Task<ApiResult<StartLoginResponse>> (StartLoginCommand command, ISender mediator)
            => await mediator.Send(command)
        ).Produces<StartLoginResponse>();

        group.MapPost("login/{id}/complete", async Task<ApiResult> (LoginId id, CompleteLoginCommand command, ISender mediator)
            => await mediator.Send(command with { Id = id })
        );
    }
}
