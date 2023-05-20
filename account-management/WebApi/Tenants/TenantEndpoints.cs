using Mapster;
using MediatR;
using PlatformPlatform.AccountManagement.Application.Tenants.Commands;
using PlatformPlatform.AccountManagement.Application.Tenants.Queries;
using PlatformPlatform.AccountManagement.Domain.Tenants;
using PlatformPlatform.Foundation.AspNetCoreUtils.Extensions;

namespace PlatformPlatform.AccountManagement.WebApi.Tenants;

public static class TenantEndpoints
{
    public static void MapTenantEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/tenants");
        group.MapGet("/{id}", GetTenant);
        group.MapPost("/", CreateTenant);
        group.MapPut("/{id}", UpdateTenant);
        group.MapDelete("/{id}", DeleteTenant);
    }

    private static async Task<IResult> GetTenant(string id, ISender sender)
    {
        var query = new GetTenantQuery(TenantId.FromString(id));
        var result = await sender.Send(query);
        return result.AsHttpResult<Tenant, TenantResponseDto>();
    }

    private static async Task<IResult> CreateTenant(CreateTenantRequest request, ISender sender)
    {
        var command = request.Adapt<CreateTenantCommand>();
        var result = await sender.Send(command);
        return result.AsHttpResult<Tenant, TenantResponseDto>($"/tenants/{result.Value?.Id}");
    }

    private static async Task<IResult> UpdateTenant(string id, UpdateTenantRequest request, ISender sender)
    {
        var command = new UpdateTenantCommand(TenantId.FromString(id), request.Name, request.Email, request.Phone);
        var result = await sender.Send(command);
        return result.AsHttpResult<Tenant, TenantResponseDto>();
    }

    private static async Task<IResult> DeleteTenant(string id, ISender sender)
    {
        var command = new DeleteTenantCommand(TenantId.FromString(id));
        var result = await sender.Send(command);
        return result.AsHttpResult<Tenant, TenantResponseDto>();
    }
}