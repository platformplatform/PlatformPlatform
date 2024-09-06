using PlatformPlatform.AccountManagement.Users.Commands;
using PlatformPlatform.AccountManagement.Users.Domain;
using PlatformPlatform.AccountManagement.Users.Queries;
using PlatformPlatform.SharedKernel.ApiResults;
using PlatformPlatform.SharedKernel.Endpoints;

namespace PlatformPlatform.AccountManagement.Api.Endpoints;

public sealed class UserEndpoints : IEndpoints
{
    private const string RoutesPrefix = "/api/account-management/users";

    public void MapEndpoints(IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup(RoutesPrefix).WithTags("Users").RequireAuthorization();

        group.MapGet("/", async Task<ApiResult<GetUsersResponseDto>> ([AsParameters] GetUsersQuery query, ISender mediator)
            => await mediator.Send(query)
        ).Produces<GetUsersResponseDto>();

        group.MapGet("/{id}", async Task<ApiResult<UserResponseDto>> (UserId id, ISender mediator)
            => await mediator.Send(new GetUserQuery(id))
        ).Produces<UserResponseDto>();

        group.MapPost("/", async Task<ApiResult> (CreateUserCommand command, ISender mediator)
            => (await mediator.Send(command)).AddResourceUri(RoutesPrefix)
        );

        group.MapPut("/{id}", async Task<ApiResult> (UserId id, UpdateUserCommand command, ISender mediator)
            => await mediator.Send(command with { Id = id })
        );

        group.MapPut("/{id}/change-user-role", async Task<ApiResult> (UserId id, ChangeUserRoleCommand command, ISender mediator)
            => await mediator.Send(command with { Id = id })
        );

        group.MapDelete("/{id}", async Task<ApiResult> (UserId id, ISender mediator)
            => await mediator.Send(new DeleteUserCommand(id))
        );

        // The id should be inferred from the authenticated user
        group.MapPost("/{id}/update-avatar", async Task<ApiResult> (UserId id, IFormFile file, ISender mediator)
            => await mediator.Send(new UpdateAvatarCommand(id, file.OpenReadStream(), file.ContentType))
        ).DisableAntiforgery(); // Disable anti-forgery until we implement it

        // The id should be inferred from the authenticated user
        group.MapPost("/{id}/remove-avatar", async Task<ApiResult> (UserId id, ISender mediator)
            => await mediator.Send(new RemoveAvatarCommand(id))
        );
    }
}
