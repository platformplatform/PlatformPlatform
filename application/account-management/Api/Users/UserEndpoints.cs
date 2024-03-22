using PlatformPlatform.AccountManagement.Application.Users;
using PlatformPlatform.SharedKernel.ApiCore.ApiResults;
using PlatformPlatform.SharedKernel.ApiCore.Endpoints;

namespace PlatformPlatform.AccountManagement.Api.Users;

[UsedImplicitly]
public class UserEndpoints : IEndpoints
{
    private const string RoutesPrefix = "/api/users";

    public void MapEndpoints(IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup(RoutesPrefix);

        group.MapGet("/", async Task<ApiResult<SearchUsersDto>> ([AsParameters] GetUsersQuery query, ISender mediator)
            => await mediator.Send(query));

        group.MapGet("/{id}",
            async Task<ApiResult<UserResponseDto>> ([AsParameters] GetUserQuery query, ISender mediator)
                => await mediator.Send(query));

        group.MapPost("/", async Task<ApiResult> (CreateUserCommand command, ISender mediator)
            => (await mediator.Send(command)).AddResourceUri(RoutesPrefix));

        group.MapPut("/{id}", async Task<ApiResult> (UserId id, UpdateUserCommand command, ISender mediator)
            => await mediator.Send(command with { Id = id }));

        group.MapDelete("/{id}", async Task<ApiResult> ([AsParameters] DeleteUserCommand command, ISender mediator)
            => await mediator.Send(command));
    }
}