using Mapster;
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

    private static async Task<IResult> GetUser(string id, ISender mediatr)
    {
        var query = new GetUser.Query((UserId) id);
        var result = await mediatr.Send(query);
        return result.AsHttpResult<User, UserResponseDto>();
    }

    private static async Task<IResult> CreateUser(CreateUserRequest request, ISender mediatr)
    {
        var command = request.Adapt<CreateUser.Command>();
        var result = await mediatr.Send(command);
        return result.AsHttpResult<User, UserResponseDto>(RoutesPrefix);
    }

    private static async Task<IResult> UpdateUser(string id, UpdateUserRequest request, ISender mediatr)
    {
        var command = new UpdateUser.Command((UserId) id, request.Email, request.UserRole);
        var result = await mediatr.Send(command);
        return result.AsHttpResult<User, UserResponseDto>();
    }

    private static async Task<IResult> DeleteUser(string id, ISender mediatr)
    {
        var command = new DeleteUser.Command((UserId) id);
        var result = await mediatr.Send(command);
        return result.AsHttpResult<User, UserResponseDto>();
    }
}