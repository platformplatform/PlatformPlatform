using MediatR;
using PlatformPlatform.AccountManagement.Application.Users;
using PlatformPlatform.AccountManagement.Domain.Users;
using PlatformPlatform.SharedKernel.ApiCore.Extensions;

namespace PlatformPlatform.AccountManagement.Api.Users;

public static class UserEndpointsV1
{
    private const string RoutesPrefix = "/api/users/v1";

    public static void MapUserEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup(RoutesPrefix);
        group.MapGet("/{id}", GetUser);
        group.MapPost("/", CreateUser);
        group.MapPut("/{id}", UpdateUser);
        group.MapDelete("/{id}", DeleteUser);
    }

    private static async Task<IResult> GetUser(UserId id, ISender mediatr)
    {
        return (await mediatr.Send(new GetUser.Query(id))).AsHttpResult<User, UserResponseDto>();
    }

    private static async Task<IResult> CreateUser(CreateUser.Command command, ISender mediatr)
    {
        return (await mediatr.Send(command)).AsHttpResult(RoutesPrefix);
    }

    private static async Task<IResult> UpdateUser(UserId id, UpdateUser.Command command, ISender mediatr)
    {
        return (await mediatr.Send(command with {Id = id})).AsHttpResult<User, UserResponseDto>();
    }

    private static async Task<IResult> DeleteUser(UserId id, ISender mediatr)
    {
        return (await mediatr.Send(new DeleteUser.Command(id))).AsHttpResult<User, UserResponseDto>();
    }
}