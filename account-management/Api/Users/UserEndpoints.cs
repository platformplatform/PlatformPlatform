using MediatR;
using PlatformPlatform.AccountManagement.Application.Users;
using PlatformPlatform.AccountManagement.Domain.Users;
using PlatformPlatform.SharedKernel.ApiCore.ApiResults;

namespace PlatformPlatform.AccountManagement.Api.Users;

public static class UserEndpoints
{
    private const string RoutesPrefix = "/api/users";

    public static void MapUserEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup(RoutesPrefix);
        group.MapGet("/{id}", GetUser);
        group.MapPost("/", CreateUser);
        group.MapPut("/{id}", UpdateUser);
        group.MapDelete("/{id}", DeleteUser);
    }

    private static async Task<ApiResult<UserResponseDto>> GetUser(UserId id, ISender mediatr)
    {
        return await mediatr.Send(new GetUser.Query(id));
    }

    private static async Task<ApiResult> CreateUser(CreateUser.Command command, ISender mediatr)
    {
        return (await mediatr.Send(command)).AddResourceUri(RoutesPrefix);
    }

    private static async Task<ApiResult> UpdateUser(UserId id, UpdateUser.Command command, ISender mediatr)
    {
        return await mediatr.Send(command with {Id = id});
    }

    private static async Task<ApiResult> DeleteUser(UserId id, ISender mediatr)
    {
        return await mediatr.Send(new DeleteUser.Command(id));
    }
}