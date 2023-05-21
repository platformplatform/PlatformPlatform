using Mapster;
using MediatR;
using PlatformPlatform.AccountManagement.Application.Tenants.Commands;
using PlatformPlatform.AccountManagement.Application.Tenants.Queries;
using PlatformPlatform.AccountManagement.Domain.Tenants;
using PlatformPlatform.AccountManagement.WebApi.Tenants.Contracts;
using PlatformPlatform.Foundation.AspNetCoreUtils.Extensions;

namespace PlatformPlatform.AccountManagement.WebApi.Tenants;

public static class TenantEndpoints
{
    private const string RoutesPrefix = "/tenants";

    public static void MapTenantEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup(RoutesPrefix);
        group.MapGet("/{id}", GetTenant);
        group.MapPost("/", CreateTenant);
        group.MapPut("/{id}", UpdateTenant);
        group.MapDelete("/{id}", DeleteTenant);
    }

    private static async Task<IResult> GetTenant(string id, ISender sender)
    {
        var query = new GetTenantQuery((TenantId) id);
        var result = await sender.Send(query);
        return result.AsHttpResult<Tenant, TenantResponseDto>();
    }

    private static async Task<IResult> CreateTenant(CreateTenantRequest request, ISender sender)
    {
        var command = request.Adapt<CreateTenant.Command>();
        var result = await sender.Send(command);
        return result.AsHttpResult<Tenant, TenantResponseDto>($"{RoutesPrefix}/{result.Value?.Id}");
    }

    private static async Task<IResult> UpdateTenant(string id, UpdateTenantRequest request, ISender sender)
    {
        var command = new UpdateTenant.Command((TenantId) id, request.Name, request.Email, request.Phone);
        var result = await sender.Send(command);
        return result.AsHttpResult<Tenant, TenantResponseDto>();
    }

    private static async Task<IResult> DeleteTenant(string id, ISender sender)
    {
        var command = new DeleteTenant.Command((TenantId) id);
        var result = await sender.Send(command);
        return result.AsHttpResult<Tenant, TenantResponseDto>();
    }
}