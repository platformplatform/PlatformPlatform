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

    private static async Task<IResult> GetTenant(TenantId id, ISender mediatr)
    {
        return (await mediatr.Send(new GetTenant.Query(id))).AsHttpResult();
    }

    private static async Task<IResult> CreateTenant(CreateTenant.Command command, ISender mediatr)
    {
        return (await mediatr.Send(command)).AsHttpResult(RoutesPrefix);
    }

    private static async Task<IResult> UpdateTenant(TenantId id, UpdateTenant.Command command, ISender mediatr)
    {
        return (await mediatr.Send(command with {Id = id})).AsHttpResult();
    }

    private static async Task<IResult> DeleteTenant(TenantId id, ISender mediatr)
    {
        return (await mediatr.Send(new DeleteTenant.Command(id))).AsHttpResult();
    }
}