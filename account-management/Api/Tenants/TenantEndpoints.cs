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
        group.MapGet("/{id}", GetTenant);
        group.MapPost("/", CreateTenant);
        group.MapPut("/{id}", UpdateTenant);
        group.MapDelete("/{id}", DeleteTenant);
    }

    private static async Task<ApiResult<TenantResponseDto>> GetTenant(TenantId id, ISender mediator)
    {
        return await mediator.Send(new GetTenant.Query(id));
    }

    private static async Task<ApiResult> CreateTenant(CreateTenant.Command command, ISender mediator)
    {
        return (await mediator.Send(command)).AddResourceUri(RoutesPrefix);
    }

    private static async Task<ApiResult> UpdateTenant(TenantId id, UpdateTenant.Command command, ISender mediator)
    {
        return await mediator.Send(command with {Id = id});
    }

    private static async Task<ApiResult> DeleteTenant(TenantId id, ISender mediator)
    {
        return await mediator.Send(new DeleteTenant.Command(id));
    }
}