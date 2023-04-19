using MediatR;
using PlatformPlatform.AccountManagement.Application.Tenants.Commands;
using PlatformPlatform.AccountManagement.Application.Tenants.Queries;
using PlatformPlatform.AccountManagement.Domain.Tenants;

namespace PlatformPlatform.AccountManagement.WebApi.Endpoints;

public static class TenantEndpoints
{
    public static void MapTenantEndpoints(this WebApplication app)
    {
        app.MapGet("/tenants/{id}", GetTenant);
        app.MapPost("/tenants", CreateTenant);
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
        var tenantId = await sender.Send(createTenantCommand);
        return Results.Created($"/tenants/{tenantId.AsRawString()}", tenantId);
    }
}