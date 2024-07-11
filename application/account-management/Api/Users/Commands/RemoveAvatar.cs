using PlatformPlatform.AccountManagement.Api.TelemetryEvents;
using PlatformPlatform.AccountManagement.Api.Users.Domain;
using PlatformPlatform.SharedKernel.ApiCore.ApiResults;
using PlatformPlatform.SharedKernel.ApiCore.Endpoints;
using PlatformPlatform.SharedKernel.Application.Cqrs;
using PlatformPlatform.SharedKernel.Application.TelemetryEvents;

namespace PlatformPlatform.AccountManagement.Api.Users.Commands;

public sealed record RemoveAvatarCommand(UserId Id) : ICommand, IRequest<Result>;

public sealed class RemoveAvatarEndpoint : IEndpoints
{
    private const string RoutesPrefix = "/api/account-management/users";

    public void MapEndpoints(IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup(RoutesPrefix).WithTags("Users");

        // Id should be inferred from the authenticated user
        group.MapPost("/{id}/remove-avatar", async Task<ApiResult> ([AsParameters] RemoveAvatarCommand command, ISender mediator)
            => await mediator.Send(command)
        );
    }
}

public sealed class RemoveAvatarCommandHandler(UserRepository userRepository, ITelemetryEventsCollector events)
    : IRequestHandler<RemoveAvatarCommand, Result>
{
    public async Task<Result> Handle(RemoveAvatarCommand command, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(command.Id, cancellationToken);
        if (user is null) return Result.NotFound($"User with id '{command.Id}' not found.");

        user.RemoveAvatar();
        userRepository.Update(user);

        events.CollectEvent(new UserAvatarRemoved());

        return Result.Success();
    }
}
