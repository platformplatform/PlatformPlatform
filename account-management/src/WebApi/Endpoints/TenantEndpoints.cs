using MediatR;
using PlatformPlatform.AccountManagement.Application.Tenants.Commands;
using PlatformPlatform.AccountManagement.Application.Tenants.Queries;
using PlatformPlatform.AccountManagement.Domain.Tenants;

namespace PlatformPlatform.AccountManagement.WebApi.Endpoints;

public static class TenantEndpoints
{
    public static void MapTenantEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/tenants");
        group.MapGet("/{id}", GetTenant);
        group.MapPost("/", CreateTenant);
    }

    private static async Task<IResult> GetTenant(string id, ISender sender)
    {
        var tenantId = TenantId.FromString(id);
        var tenant = await sender.Send(new GetTenantByIdQuery(tenantId));
        return tenant is null ? Results.NotFound() : Results.Ok(tenant);
    }

    private static async Task<IResult> CreateTenant(CreateTenantRequest createTenantRequest, ISender sender)
    {
        var createTenantCommand = new CreateTenantCommand(createTenantRequest.Name);
        var tenant = await sender.Send(createTenantCommand);
        return Results.Created($"/tenants/{tenant.Id}", tenant);
    }
}