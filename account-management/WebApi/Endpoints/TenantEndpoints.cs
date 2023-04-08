using MediatR;
using PlatformPlatform.AccountManagement.Application.Tenants.Commands;
using PlatformPlatform.AccountManagement.Application.Tenants.Queries;

namespace PlatformPlatform.AccountManagement.WebApi.Endpoints;

public static class TenantEndpoints
{
    public static void MapTenantEndpoints(this WebApplication app)
    {
        app.MapGet("/tenants/{id:long}", GetTenant);
    }

    private static async Task<IResult> GetTenant(long id, IMediator mediator)
    {
        try
        {
            var tenant = await mediator.Send(new GetTenantByIdQuery(id));
            return Results.Ok(tenant);
        }
        catch (Exception e) when (e.Message == "TenantNotFound")
        {
            return Results.NotFound();
        }
    }
}