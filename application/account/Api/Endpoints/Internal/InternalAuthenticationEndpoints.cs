using Account.Features.Authentication.Commands;
using SharedKernel.ApiResults;
using SharedKernel.Endpoints;

namespace Account.Api.Endpoints.Internal;

public sealed class InternalAuthenticationEndpoints : IEndpoints
{
    private const string RoutesPrefix = "/internal-api/account/authentication";

    public void MapEndpoints(IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup(RoutesPrefix).WithTags("InternalAuthentication").RequireAuthorization().ProducesValidationProblem();

        // Note: This endpoint must be called with the refresh token as Bearer token in the Authorization header
        group.MapPost("/refresh-authentication-tokens", async Task<ApiResult> (IMediator mediator)
            => await mediator.Send(new RefreshAuthenticationTokensCommand())
        ).DisableAntiforgery();
    }
}
