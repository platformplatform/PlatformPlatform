using Account.Features.Authentication.Commands;
using Account.Features.Authentication.Queries;
using SharedKernel.ApiResults;
using SharedKernel.Authentication.TokenGeneration;
using SharedKernel.Endpoints;

namespace Account.Api.Endpoints;

public sealed class AuthenticationEndpoints : IEndpoints
{
    private const string RoutesPrefix = "/api/account/authentication";

    public void MapEndpoints(IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup(RoutesPrefix).WithTags("Authentication").RequireAuthorization().ProducesValidationProblem();

        group.MapPost("/logout", async Task<ApiResult> (IMediator mediator)
            => await mediator.Send(new LogoutCommand())
        );

        group.MapPost("/switch-tenant", async Task<ApiResult> (SwitchTenantCommand command, IMediator mediator)
            => await mediator.Send(command)
        );

        group.MapGet("/sessions", async Task<ApiResult<UserSessionsResponse>> ([AsParameters] GetUserSessionsQuery query, IMediator mediator)
            => await mediator.Send(query)
        ).Produces<UserSessionsResponse>();

        group.MapDelete("/sessions/{id}", async Task<ApiResult> (SessionId id, IMediator mediator)
            => await mediator.Send(new RevokeSessionCommand { Id = id })
        );

        // Note: This endpoint must be called with the refresh token as Bearer token in the Authorization header
        routes.MapPost("/internal-api/account/authentication/refresh-authentication-tokens", async Task<ApiResult> (IMediator mediator)
            => await mediator.Send(new RefreshAuthenticationTokensCommand())
        ).DisableAntiforgery();
    }
}
