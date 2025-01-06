using PlatformPlatform.AccountManagement.Features.Authentication.Commands;
using PlatformPlatform.AccountManagement.Features.Authentication.Domain;
using PlatformPlatform.SharedKernel.ApiResults;
using PlatformPlatform.SharedKernel.Endpoints;

namespace PlatformPlatform.AccountManagement.Api.Endpoints;

public sealed class AuthenticationEndpoints : IEndpoints
{
    private const string RoutesPrefix = "/api/account-management/authentication";

    public void MapEndpoints(IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup(RoutesPrefix).WithTags("Authentication").RequireAuthorization();

        group.MapPost("/login/start", async Task<ApiResult<StartLoginResponse>> (StartLoginCommand command, IMediator mediator)
            => await mediator.Send(command)
        ).Produces<StartLoginResponse>().AllowAnonymous();

        group.MapPost("login/{id}/complete", async Task<ApiResult> (LoginId id, CompleteLoginCommand command, IMediator mediator)
            => await mediator.Send(command with { Id = id })
        ).AllowAnonymous();

        group.MapPost("login/{id}/resend", async Task<ApiResult<ResendLoginCodeResponse>> (LoginId id, IMediator mediator)
            => await mediator.Send(new ResendLoginCodeCommand { Id = id })
        ).Produces<ResendLoginCodeResponse>().AllowAnonymous();

        group.MapPost("logout", async Task<ApiResult> (IMediator mediator)
            => await mediator.Send(new LogoutCommand())
        );

        // Note: This endpoint must be called with the refresh token as Bearer token in the Authorization header
        group.MapPost("refresh-authentication-tokens", async Task<ApiResult> (IMediator mediator)
            => await mediator.Send(new RefreshAuthenticationTokensCommand())
        );
    }
}
