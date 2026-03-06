using Account.Features.Users.Commands;
using Account.Features.Users.Shared;
using SharedKernel.ApiResults;
using SharedKernel.Domain;
using SharedKernel.Endpoints;

namespace Account.Api.Endpoints;

public sealed class UserEndpoints : IEndpoints
{
    private const string RoutesPrefix = "/api/account/users";

    public void MapEndpoints(IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup(RoutesPrefix).WithTags("Users").RequireAuthorization().ProducesValidationProblem();

        group.MapDelete("/{id}", async Task<ApiResult<UserResponse>> (UserId id, IMediator mediator)
            => await mediator.Send(new DeleteUserCommand(id))
        ).Produces<UserResponse>();

        group.MapPost("/bulk-delete", async Task<ApiResult<UserResponse[]>> (BulkDeleteUsersCommand command, IMediator mediator)
            => await mediator.Send(command)
        ).Produces<UserResponse[]>();

        group.MapPut("/{id}/change-user-role", async Task<ApiResult<UserResponse>> (UserId id, ChangeUserRoleCommand command, IMediator mediator)
            => await mediator.Send(command with { Id = id })
        ).Produces<UserResponse>();

        group.MapPost("/invite", async Task<ApiResult<UserResponse>> (InviteUserCommand command, IMediator mediator)
            => await mediator.Send(command)
        ).Produces<UserResponse>();

        group.MapPost("/decline-invitation", async Task<ApiResult> (DeclineInvitationCommand command, IMediator mediator)
            => await mediator.Send(command)
        );

        group.MapPost("/{id}/restore", async Task<ApiResult<UserResponse>> (UserId id, IMediator mediator)
            => await mediator.Send(new RestoreUserCommand(id))
        ).Produces<UserResponse>();

        group.MapDelete("/{id}/purge", async Task<ApiResult> (UserId id, IMediator mediator)
            => await mediator.Send(new PurgeUserCommand(id))
        );

        group.MapPost("/deleted/bulk-purge", async Task<ApiResult> (BulkPurgeUsersCommand command, IMediator mediator)
            => await mediator.Send(command)
        );

        group.MapPost("/deleted/empty-recycle-bin", async Task<ApiResult<int>> (IMediator mediator)
            => await mediator.Send(new EmptyRecycleBinCommand())
        ).Produces<int>();

        // The following endpoints are for the current user only
        group.MapPut("/me", async Task<ApiResult<UserResponse>> (UpdateCurrentUserCommand command, IMediator mediator)
            => (await mediator.Send(command)).AddRefreshAuthenticationTokens()
        ).Produces<UserResponse>();

        group.MapPost("/me/update-avatar", async Task<ApiResult<UserResponse>> (IFormFile file, IMediator mediator)
            => await mediator.Send(new UpdateAvatarCommand(file.OpenReadStream(), file.ContentType))
        ).Produces<UserResponse>();

        group.MapDelete("/me/remove-avatar", async Task<ApiResult<UserResponse>> (IMediator mediator)
            => await mediator.Send(new RemoveAvatarCommand())
        ).Produces<UserResponse>();

        group.MapPut("/me/change-locale", async Task<ApiResult<UserResponse>> (ChangeLocaleCommand command, IMediator mediator)
            => (await mediator.Send(command)).AddRefreshAuthenticationTokens()
        ).Produces<UserResponse>();

        group.MapPut("/me/change-zoom-level", async Task<ApiResult> (ChangeZoomLevelCommand command, IMediator mediator)
            => await mediator.Send(command)
        );

        group.MapPut("/me/change-theme", async Task<ApiResult> (ChangeThemeCommand command, IMediator mediator)
            => await mediator.Send(command)
        );
    }
}
