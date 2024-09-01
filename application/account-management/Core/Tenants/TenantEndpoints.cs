using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using PlatformPlatform.AccountManagement.Core.Tenants.Commands;
using PlatformPlatform.AccountManagement.Core.Tenants.Domain;
using PlatformPlatform.AccountManagement.Core.Tenants.Queries;
using PlatformPlatform.SharedKernel.ApiResults;
using PlatformPlatform.SharedKernel.Endpoints;

namespace PlatformPlatform.AccountManagement.Core.Tenants;

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
