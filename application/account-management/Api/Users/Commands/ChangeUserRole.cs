using PlatformPlatform.AccountManagement.Api.TelemetryEvents;
using PlatformPlatform.AccountManagement.Api.Users.Domain;
using PlatformPlatform.SharedKernel.ApiCore.ApiResults;
using PlatformPlatform.SharedKernel.ApiCore.Endpoints;
using PlatformPlatform.SharedKernel.Application.Cqrs;
using PlatformPlatform.SharedKernel.Application.TelemetryEvents;

namespace PlatformPlatform.AccountManagement.Api.Users.Commands;

public sealed record ChangeUserRoleCommand : ICommand, IRequest<Result>
{
    [JsonIgnore] // Removes the Id from the API contract
    public UserId Id { get; init; } = null!;

    public required UserRole UserRole { get; init; }
}

public sealed class ChangeUserRoleEndpoint : IEndpoints
{
    private const string RoutesPrefix = "/api/account-management/users";

    public void MapEndpoints(IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup(RoutesPrefix).WithTags("Users");

        group.MapPut("/{id}/change-user-role", async Task<ApiResult> (UserId id, ChangeUserRoleCommand command, ISender mediator)
            => await mediator.Send(command with { Id = id })
        );
    }
}

public sealed class ChangeUserRoleHandler(UserRepository userRepository, ITelemetryEventsCollector events)
    : IRequestHandler<ChangeUserRoleCommand, Result>
{
    public async Task<Result> Handle(ChangeUserRoleCommand command, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(command.Id, cancellationToken);
        if (user is null) return Result.NotFound($"User with id '{command.Id}' not found.");

        var oldUserRole = user.Role;
        user.ChangeUserRole(command.UserRole);
        userRepository.Update(user);

        events.CollectEvent(new UserRoleChanged(oldUserRole, command.UserRole));

        return Result.Success();
    }
}
