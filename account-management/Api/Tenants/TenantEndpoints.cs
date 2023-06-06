using MediatR;
using PlatformPlatform.AccountManagement.Application.Tenants;
using PlatformPlatform.AccountManagement.Domain.Tenants;
using PlatformPlatform.SharedKernel.ApiCore.HttpResults;

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

    private static async Task<ApiResult<TenantResponseDto>> GetTenant(TenantId id, ISender mediatr)
    {
        return await mediatr.Send(new GetTenant.Query(id));
    }

    private static async Task<ApiResult<TenantResponseDto>> CreateTenant(CreateTenant.Command command, ISender mediatr)
    {
        return (await mediatr.Send(command)).AddResourceUri(RoutesPrefix);
    }

    private static async Task<ApiResult<TenantResponseDto>> UpdateTenant(TenantId id, UpdateTenant.Command command,
        ISender mediatr)
    {
        return await mediatr.Send(command with {Id = id});
    }

    private static async Task<ApiResult<TenantResponseDto>> DeleteTenant(TenantId id, ISender mediatr)
    {
        return await mediatr.Send(new DeleteTenant.Command(id));
    }
}