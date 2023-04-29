using MediatR;
using PlatformPlatform.AccountManagement.Application.Tenants.Commands.CreateTenant;
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
        var getTenantByIdQuery = new GetTenantByIdQuery(TenantId.FromString(id));
        var getTenantByIdQueryResult = await sender.Send(getTenantByIdQuery);
        return getTenantByIdQueryResult.IsSuccess
            ? Results.Ok(getTenantByIdQueryResult.Value)
            : Results.NotFound(getTenantByIdQueryResult.Errors);
    }

    private static async Task<IResult> CreateTenant(CreateTenantCommand createTenantCommand, ISender sender)
    {
        var createTenantCommandResult = await sender.Send(createTenantCommand);
        return createTenantCommandResult.IsSuccess
            ? Results.Created($"/tenants/{createTenantCommandResult.Value.Id}", createTenantCommandResult.Value)
            : Results.BadRequest(createTenantCommandResult.Errors);
    }
}