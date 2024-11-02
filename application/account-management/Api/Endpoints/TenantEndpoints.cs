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

        group.MapGet("/{id}", async Task<ApiResult<TenantResponse>> (TenantId id, IMediator mediator)
            => await mediator.Send(new GetTenantQuery(id))
        ).Produces<TenantResponse>();

        group.MapPut("/{id}", async Task<ApiResult> (TenantId id, UpdateTenantCommand command, IMediator mediator)
            => await mediator.Send(command with { Id = id })
        );

        group.MapDelete("/{id}", async Task<ApiResult> (TenantId id, IMediator mediator)
            => await mediator.Send(new DeleteTenantCommand(id))
        );
    }
}
