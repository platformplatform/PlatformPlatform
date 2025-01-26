using PlatformPlatform.AccountManagement.Features.Tenants.Commands;
using PlatformPlatform.AccountManagement.Features.Tenants.Queries;
using PlatformPlatform.SharedKernel.ApiResults;
using PlatformPlatform.SharedKernel.Domain;
using PlatformPlatform.SharedKernel.Endpoints;

namespace PlatformPlatform.AccountManagement.Api.Endpoints;

public sealed class TenantEndpoints : IEndpoints
{
    private const string RoutesPrefix = "/api/account-management/tenants";

    public void MapEndpoints(IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup(RoutesPrefix).WithTags("Tenants").RequireAuthorization();

        group.MapGet("/current", async Task<ApiResult<TenantResponse>> (IMediator mediator)
            => await mediator.Send(new GetTenantQuery())
        ).Produces<TenantResponse>();

        group.MapPut("/current", async Task<ApiResult> (UpdateCurrentTenantCommand command, IMediator mediator)
            => await mediator.Send(command)
        );

        routes.MapDelete("/internal-api/account-management/tenants/{id}", async Task<ApiResult> (TenantId id, IMediator mediator)
            => await mediator.Send(new DeleteTenantCommand(id))
        );
    }
}
