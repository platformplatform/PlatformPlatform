using Account.Features.Users.Commands;
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

        group.MapDelete("/{id}", async Task<ApiResult> (UserId id, IMediator mediator)
            => await mediator.Send(new DeleteUserCommand(id))
        );

        group.MapPost("/bulk-delete", async Task<ApiResult> (BulkDeleteUsersCommand command, IMediator mediator)
            => await mediator.Send(command)
        );

        group.MapPut("/{id}/change-user-role", async Task<ApiResult> (UserId id, ChangeUserRoleCommand command, IMediator mediator)
            => await mediator.Send(command with { Id = id })
        );

        group.MapPost("/invite", async Task<ApiResult> (InviteUserCommand command, IMediator mediator)
            => await mediator.Send(command)
        );

        group.MapPost("/decline-invitation", async Task<ApiResult> (DeclineInvitationCommand command, IMediator mediator)
            => await mediator.Send(command)
        );

        group.MapPost("/{id}/restore", async Task<ApiResult> (UserId id, IMediator mediator)
            => await mediator.Send(new RestoreUserCommand(id))
        );

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
        group.MapPut("/me", async Task<ApiResult> (UpdateCurrentUserCommand command, IMediator mediator)
            => (await mediator.Send(command)).AddRefreshAuthenticationTokens()
        );

        group.MapPost("/me/update-avatar", async Task<ApiResult> (IFormFile file, IMediator mediator)
            => await mediator.Send(new UpdateAvatarCommand(file.OpenReadStream(), file.ContentType))
        );

        group.MapDelete("/me/remove-avatar", async Task<ApiResult> (IMediator mediator)
            => await mediator.Send(new RemoveAvatarCommand())
        );

        group.MapPut("/me/change-locale", async Task<ApiResult> (ChangeLocaleCommand command, IMediator mediator)
            => (await mediator.Send(command)).AddRefreshAuthenticationTokens()
        );

        group.MapPut("/me/change-zoom-level", async Task<ApiResult> (ChangeZoomLevelCommand command, IMediator mediator)
            => await mediator.Send(command)
        );

        group.MapPut("/me/change-theme", async Task<ApiResult> (ChangeThemeCommand command, IMediator mediator)
            => await mediator.Send(command)
        );
    }
}
