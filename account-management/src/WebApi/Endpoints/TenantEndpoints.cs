using MediatR;
using PlatformPlatform.AccountManagement.Application.Tenants.Commands.CreateTenant;
using PlatformPlatform.AccountManagement.Application.Tenants.Commands.DeleteTenant;
using PlatformPlatform.AccountManagement.Application.Tenants.Commands.UpdateTenant;
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
        group.MapPut("/{id}", UpdateTenant);
        group.MapDelete("/{id}", DeleteTenant);
    }

    private static async Task<IResult> GetTenant(string id, ISender sender)
    {
        var getTenantByIdQueryResult = await sender.Send(new GetTenantByIdQuery(TenantId.FromString(id)));
        return getTenantByIdQueryResult.IsSuccess
            ? Results.Ok(getTenantByIdQueryResult.Value)
            : Results.NotFound(getTenantByIdQueryResult.Error);
    }

    private static async Task<IResult> CreateTenant(CreateTenantCommand createTenantCommand, ISender sender)
    {
        var createTenantCommandResult = await sender.Send(createTenantCommand);
        return createTenantCommandResult.IsSuccess
            ? Results.Created($"/tenants/{createTenantCommandResult.Value.Id}", createTenantCommandResult.Value)
            : Results.BadRequest(createTenantCommandResult.Errors);
    }

    private static async Task<IResult> UpdateTenant(string id, UpdateTenantRequest request, ISender sender)
    {
        var command = new UpdateTenantCommand(TenantId.FromString(id), request.Name, request.Email, request.Phone);
        var result = await sender.Send(command);
        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.BadRequest(result.Errors);
    }

    private static async Task<IResult> DeleteTenant(string id, ISender sender)
    {
        var result = await sender.Send(new DeleteTenantCommand(TenantId.FromString(id)));
        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.BadRequest(result.Errors);
    }
}

public sealed record UpdateTenantRequest(string Name, string Email, string? Phone);