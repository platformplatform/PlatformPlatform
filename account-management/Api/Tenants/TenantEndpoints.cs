using MediatR;
using PlatformPlatform.AccountManagement.Application.Tenants;
using PlatformPlatform.AccountManagement.Domain.Tenants;
using PlatformPlatform.SharedKernel.ApiCore.ApiResults;

namespace PlatformPlatform.AccountManagement.Api.Tenants;

public static class TenantEndpoints
{
    private const string RoutesPrefix = "/api/tenants";

    public static void MapTenantEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup(RoutesPrefix);

        group.MapGet("/{id}", async Task<ApiResult<TenantResponseDto>> (TenantId id, ISender mediator)
            => await mediator.Send(new GetTenant.Query(id)));

        group.MapPost("/", async Task<ApiResult> (CreateTenant.Command command, ISender mediator)
            => (await mediator.Send(command)).AddResourceUri(RoutesPrefix));

        group.MapPut("/{id}", async Task<ApiResult> (TenantId id, UpdateTenant.Command command, ISender mediator)
            => await mediator.Send(command with {Id = id}));

        group.MapDelete("/{id}", async Task<ApiResult> (TenantId id, ISender mediator)
            => await mediator.Send(new DeleteTenant.Command(id)));
    }
}