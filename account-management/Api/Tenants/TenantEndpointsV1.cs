using Mapster;
using MediatR;
using PlatformPlatform.AccountManagement.Application.Tenants;
using PlatformPlatform.AccountManagement.Domain.Tenants;
using PlatformPlatform.SharedKernel.ApiCore.Extensions;

namespace PlatformPlatform.AccountManagement.Api.Tenants;

public static class TenantEndpointsV1
{
    private const string RoutesPrefix = "/api/tenants/v1";

    public static void MapTenantEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup(RoutesPrefix);
        group.MapGet("/{id}", GetTenant);
        group.MapPost("/", CreateTenant);
        group.MapPut("/{id}", UpdateTenant);
        group.MapDelete("/{id}", DeleteTenant);
    }

    private static async Task<IResult> GetTenant(string id, ISender mediatr)
    {
        return (await mediatr.Send(new GetTenant.Query((TenantId) id)))
            .AsHttpResult<Tenant, TenantResponseDto>();
    }

    private static async Task<IResult> CreateTenant(CreateTenantRequest request, ISender mediatr)
    {
        return (await mediatr.Send(request.Adapt<CreateTenant.Command>()))
            .AsHttpResult(RoutesPrefix);
    }

    private static async Task<IResult> UpdateTenant(string id, UpdateTenantRequest request, ISender mediatr)
    {
        return (await mediatr.Send(new UpdateTenant.Command((TenantId) id, request.Name, request.Email, request.Phone)))
            .AsHttpResult<Tenant, TenantResponseDto>();
    }

    private static async Task<IResult> DeleteTenant(string id, ISender mediatr)
    {
        return (await mediatr.Send(new DeleteTenant.Command((TenantId) id)))
            .AsHttpResult<Tenant, TenantResponseDto>();
    }
}