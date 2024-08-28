using PlatformPlatform.AccountManagement.Core.Authentication.Commands;
using PlatformPlatform.AccountManagement.Core.Authentication.Domain;
using PlatformPlatform.SharedKernel.ApiCore.ApiResults;
using PlatformPlatform.SharedKernel.ApiCore.Endpoints;

namespace PlatformPlatform.AccountManagement.Api.Authentication;

public class AuthenticationEndpoints : IEndpoints
{
    private const string RoutesPrefix = "/api/account-management/authentication";

    public void MapEndpoints(IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup(RoutesPrefix).WithTags("Authentication").RequireAuthorization();

        group.MapPost("/login/start", async Task<ApiResult<StartLoginResponse>> (StartLoginCommand command, ISender mediator)
            => await mediator.Send(command)
        ).Produces<StartLoginResponse>().AllowAnonymous();

        group.MapPost("login/{id}/complete", async Task<ApiResult> (LoginId id, CompleteLoginCommand command, ISender mediator)
            => await mediator.Send(command with { Id = id })
        ).AllowAnonymous();

        group.MapPost("logout", async Task<ApiResult> (ISender mediator)
            => await mediator.Send(new LogoutCommand())
        ).AllowAnonymous();

        // Note: This endpoint must be called with the refresh token as Bear token in the Authorization header
        group.MapPost("refresh-authentication-tokens", async Task<ApiResult> (ISender mediator)
            => await mediator.Send(new RefreshAuthenticationTokens())
        );
    }
}
