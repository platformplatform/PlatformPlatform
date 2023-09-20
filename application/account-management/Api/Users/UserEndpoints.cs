using PlatformPlatform.AccountManagement.Application.Users;
using PlatformPlatform.SharedKernel.ApiCore.ApiResults;

namespace PlatformPlatform.AccountManagement.Api.Users;

public static class UserEndpoints
{
    private const string RoutesPrefix = "/api/users";

    public static void MapUserEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup(RoutesPrefix);

        group.MapGet("/{id}", async Task<ApiResult<UserResponseDto>> (UserId id, ISender mediator)
            => await mediator.Send(new GetUser.Query(id)));

        group.MapPost("/", async Task<ApiResult> (CreateUser.Command command, ISender mediator)
            => (await mediator.Send(command)).AddResourceUri(RoutesPrefix));

        group.MapPut("/{id}", async Task<ApiResult> (UserId id, UpdateUser.Command command, ISender mediator)
            => await mediator.Send(command with {Id = id}));

        group.MapDelete("/{id}", async Task<ApiResult> (UserId id, ISender mediator)
            => await mediator.Send(new DeleteUser.Command(id)));
    }
}