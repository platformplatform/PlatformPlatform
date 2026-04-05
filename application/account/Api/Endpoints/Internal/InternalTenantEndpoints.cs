using Account.Features.Tenants.Commands;
using Account.Features.Tenants.Queries;
using SharedKernel.ApiResults;
using SharedKernel.Domain;
using SharedKernel.Endpoints;

namespace Account.Api.Endpoints.Internal;

public sealed class InternalTenantEndpoints : IEndpoints
{
    public void MapEndpoints(IEndpointRouteBuilder routes)
    {
        routes.MapGet("/internal-api/account/tenants", async Task<ApiResult<GetTenantsResponse>> (IMediator mediator)
            => await mediator.Send(new GetTenantsQuery())
        ).Produces<GetTenantsResponse>();

        routes.MapDelete("/internal-api/account/tenants/{id}", async Task<ApiResult> (TenantId id, IMediator mediator)
            => await mediator.Send(new DeleteTenantCommand(id))
        );
    }
}
