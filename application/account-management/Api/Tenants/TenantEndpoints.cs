using PlatformPlatform.AccountManagement.Application.Tenants;
using PlatformPlatform.SharedKernel.ApiCore.ApiResults;
using PlatformPlatform.SharedKernel.ApiCore.Endpoints;

namespace PlatformPlatform.AccountManagement.Api.Tenants;

[UsedImplicitly]
public class TenantEndpoints : IEndpoints
{
    private const string RoutesPrefix = "/api/tenants";

    public void MapEndpoints(IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup(RoutesPrefix);

        group.MapGet("/{id}",
            async Task<ApiResult<TenantResponseDto>> ([AsParameters] GetTenantQuery query, ISender mediator)
                => await mediator.Send(query));

        group.MapPut("/{id}", async Task<ApiResult> (TenantId id, UpdateTenantCommand command, ISender mediator)
            => await mediator.Send(command with { Id = id }));

        group.MapDelete("/{id}", async Task<ApiResult> ([AsParameters] DeleteTenantCommand command, ISender mediator)
            => await mediator.Send(command));
    }
}