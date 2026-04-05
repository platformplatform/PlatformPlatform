using Account.Features.Tenants.Commands;
using Account.Features.Tenants.Queries;
using SharedKernel.ApiResults;
using SharedKernel.Domain;
using SharedKernel.Endpoints;

namespace Account.Api.Endpoints.Internal;

public sealed class InternalTenantEndpoints : IEndpoints
{
    private const string RoutesPrefix = "/internal-api/account/tenants";

    public void MapEndpoints(IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup(RoutesPrefix).WithTags("InternalTenants").ProducesValidationProblem();

        group.MapGet("/", async Task<ApiResult<GetTenantsResponse>> (IMediator mediator)
            => await mediator.Send(new GetTenantsQuery())
        ).Produces<GetTenantsResponse>();

        group.MapDelete("/{id}", async Task<ApiResult> (TenantId id, IMediator mediator)
            => await mediator.Send(new DeleteTenantCommand(id))
        );
    }
}
