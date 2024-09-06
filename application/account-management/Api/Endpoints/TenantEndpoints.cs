using PlatformPlatform.AccountManagement.Tenants.Commands;
using PlatformPlatform.AccountManagement.Tenants.Domain;
using PlatformPlatform.AccountManagement.Tenants.Queries;
using PlatformPlatform.SharedKernel.ApiResults;
using PlatformPlatform.SharedKernel.Endpoints;

namespace PlatformPlatform.AccountManagement.Api.Endpoints;

public sealed class TenantEndpoints : IEndpoints
{
    private const string RoutesPrefix = "/api/account-management/tenants";

    public void MapEndpoints(IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup(RoutesPrefix).WithTags("Tenants").RequireAuthorization();

        group.MapGet("/{id}", async Task<ApiResult<TenantResponseDto>> ([AsParameters] GetTenantQuery query, ISender mediator)
            => await mediator.Send(query)
        ).Produces<TenantResponseDto>();

        group.MapPut("/{id}", async Task<ApiResult> (TenantId id, UpdateTenantCommand command, ISender mediator)
            => await mediator.Send(command with { Id = id })
        );

        group.MapDelete("/{id}", async Task<ApiResult> ([AsParameters] DeleteTenantCommand command, ISender mediator)
            => await mediator.Send(command)
        );
    }
}
