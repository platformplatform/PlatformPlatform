using PlatformPlatform.AccountManagement.Features.Users.Commands;
using PlatformPlatform.AccountManagement.Features.Users.Queries;
using PlatformPlatform.SharedKernel.ApiResults;
using PlatformPlatform.SharedKernel.Domain;
using PlatformPlatform.SharedKernel.Endpoints;

namespace PlatformPlatform.AccountManagement.Api.Endpoints;

public sealed class UserEndpoints : IEndpoints
{
    private const string RoutesPrefix = "/api/account-management/users";

    public void MapEndpoints(IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup(RoutesPrefix).WithTags("Users").RequireAuthorization();

        group.MapGet("/", async Task<ApiResult<GetUsersResponse>> ([AsParameters] GetUsersQuery query, IMediator mediator)
            => await mediator.Send(query)
        ).Produces<GetUsersResponse>();

        group.MapGet("/summary", async Task<ApiResult<GetUserSummaryResponse>> (IMediator mediator)
            => await mediator.Send(new GetUserSummaryQuery())
        ).Produces<GetUserSummaryResponse>();

        group.MapGet("/{id}", async Task<ApiResult<UserResponse>> ([AsParameters] GetUserQuery query, IMediator mediator)
            => await mediator.Send(query)
        ).Produces<UserResponse>();

        group.MapPost("/", async Task<ApiResult> (CreateUserCommand command, IMediator mediator)
            => (await mediator.Send(command)).AddResourceUri(RoutesPrefix)
        );

        group.MapDelete("/{id}", async Task<ApiResult> (UserId id, IMediator mediator)
            => await mediator.Send(new DeleteUserCommand(id))
        );

        group.MapPut("/{id}/change-user-role", async Task<ApiResult> (UserId id, ChangeUserRoleCommand command, IMediator mediator)
            => await mediator.Send(command with { Id = id })
        );

        group.MapPost("/invite", async Task<ApiResult> (InviteUserCommand command, IMediator mediator)
            => await mediator.Send(command)
        );

        // The following endpoints are for the current user only
        group.MapPut("/", async Task<ApiResult> (UpdateUserCommand command, IMediator mediator)
            => (await mediator.Send(command)).AddRefreshAuthenticationTokens()
        );

        group.MapPost("/update-avatar", async Task<ApiResult> (IFormFile file, IMediator mediator)
            => await mediator.Send(new UpdateAvatarCommand(file.OpenReadStream(), file.ContentType))
        ).DisableAntiforgery(); // Disable anti-forgery until we implement it

        group.MapDelete("/remove-avatar", async Task<ApiResult> (IMediator mediator)
            => await mediator.Send(new RemoveAvatarCommand())
        );

        group.MapPut("/change-locale", async Task<ApiResult> (ChangeLocaleCommand command, IMediator mediator)
            => (await mediator.Send(command)).AddRefreshAuthenticationTokens()
        );
    }
}
