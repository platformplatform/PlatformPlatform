using PlatformPlatform.AccountManagement.Application.Tenants;
using PlatformPlatform.SharedKernel.ApiCore.ApiResults;

namespace PlatformPlatform.AccountManagement.Api.Tenants;

public static class TenantEndpoints
{
    private const string RoutesPrefix = "/api/tenants";

    public static void MapTenantEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup(RoutesPrefix);

        group.MapGet("/{id}", async Task<ApiResult<TenantResponseDto>> (TenantId id, ISender mediator)
            => await mediator.Send(new GetTenantQuery(id)));

        group.MapPut("/{id}", async Task<ApiResult> (TenantId id, UpdateTenantCommand command, ISender mediator)
            => await mediator.Send(command with { Id = id }));

        group.MapDelete("/{id}", async Task<ApiResult> (TenantId id, ISender mediator)
            => await mediator.Send(new DeleteTenantCommand(id)));
    }
}