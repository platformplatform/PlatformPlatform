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
        var tenantDto = await sender.Send(new GetTenantByIdQuery(tenantId));
        return tenantDto is null ? Results.NotFound() : Results.Ok(tenantDto);
    }

    private static async Task<IResult> CreateTenant(CreateTenantRequest createTenantRequest, ISender sender)
    {
        var createTenantCommand = new CreateTenantCommand(createTenantRequest.Name);
        var tenantDto = await sender.Send(createTenantCommand);
        return Results.Created($"/tenants/{tenantDto.Id}", tenantDto);
    }
}