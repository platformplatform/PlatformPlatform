using PlatformPlatform.Account.Features.Users.Commands;
using PlatformPlatform.Account.Features.Users.Queries;
using PlatformPlatform.SharedKernel.ApiResults;
using PlatformPlatform.SharedKernel.Domain;
using PlatformPlatform.SharedKernel.Endpoints;

namespace PlatformPlatform.Account.Api.Endpoints;

public sealed class UserEndpoints : IEndpoints
{
    private const string RoutesPrefix = "/api/account/users";

    public void MapEndpoints(IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup(RoutesPrefix).WithTags("Users").RequireAuthorization().ProducesValidationProblem();

        group.MapGet("/", async Task<ApiResult<UsersResponse>> ([AsParameters] GetUsersQuery query, IMediator mediator)
            => await mediator.Send(query)
        ).Produces<UsersResponse>();

        group.MapGet("/{id}", async Task<ApiResult<UserDetails>> (UserId id, IMediator mediator)
            => await mediator.Send(new GetUserByIdQuery(id))
        ).Produces<UserDetails>();

        group.MapGet("/summary", async Task<ApiResult<UserSummaryResponse>> (IMediator mediator)
            => await mediator.Send(new GetUserSummaryQuery())
        ).Produces<UserSummaryResponse>();

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

        group.MapGet("/deleted", async Task<ApiResult<DeletedUsersResponse>> ([AsParameters] GetDeletedUsersQuery query, IMediator mediator)
            => await mediator.Send(query)
        ).Produces<DeletedUsersResponse>();

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
        group.MapGet("/me", async Task<ApiResult<CurrentUserResponse>> ([AsParameters] GetUserQuery query, IMediator mediator)
            => await mediator.Send(query)
        ).Produces<CurrentUserResponse>();

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
    }
}
