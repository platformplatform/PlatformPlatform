using Account.Features.Tenants.Commands;
using Account.Features.Tenants.Queries;
using Account.Features.Tenants.Shared;
using SharedKernel.ApiResults;
using SharedKernel.Domain;
using SharedKernel.Endpoints;

namespace Account.Api.Endpoints;

public sealed class TenantEndpoints : IEndpoints
{
    private const string RoutesPrefix = "/api/account/tenants";

    public void MapEndpoints(IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup(RoutesPrefix).WithTags("Tenants").RequireAuthorization().ProducesValidationProblem();

        group.MapPut("/current", async Task<ApiResult<TenantResponse>> (UpdateCurrentTenantCommand command, IMediator mediator)
            => (await mediator.Send(command)).AddRefreshAuthenticationTokens()
        ).Produces<TenantResponse>();

        group.MapGet("/", async Task<ApiResult<GetTenantsForUserResponse>> (IMediator mediator)
            => await mediator.Send(new GetTenantsForUserQuery())
        ).Produces<GetTenantsForUserResponse>();

        group.MapPost("/current/update-logo", async Task<ApiResult<TenantResponse>> (IFormFile file, IMediator mediator)
            => await mediator.Send(new UpdateTenantLogoCommand(file.OpenReadStream(), file.ContentType))
        ).Produces<TenantResponse>().DisableAntiforgery();

        group.MapDelete("/current/remove-logo", async Task<ApiResult<TenantResponse>> (IMediator mediator)
            => await mediator.Send(new RemoveTenantLogoCommand())
        ).Produces<TenantResponse>();

        routes.MapDelete("/internal-api/account/tenants/{id}", async Task<ApiResult> (TenantId id, IMediator mediator)
            => await mediator.Send(new DeleteTenantCommand(id))
        );
    }
}
