using Account.Features.Users.Domain;
using Account.Features.Users.Shared;
using JetBrains.Annotations;
using SharedKernel.Cqrs;
using SharedKernel.Domain;
using SharedKernel.ExecutionContext;
using SharedKernel.Telemetry;

namespace Account.Features.Users.Commands;

[PublicAPI]
public sealed record ChangeUserRoleCommand : ICommand, IRequest<Result<UserResponse>>
{
    [JsonIgnore] // Removes this property from the API contract
    public UserId Id { get; init; } = null!;

    public required UserRole UserRole { get; init; }
}

public sealed class ChangeUserRoleHandler(IUserRepository userRepository, IExecutionContext executionContext, ITelemetryEventsCollector events)
    : IRequestHandler<ChangeUserRoleCommand, Result<UserResponse>>
{
    public async Task<Result<UserResponse>> Handle(ChangeUserRoleCommand command, CancellationToken cancellationToken)
    {
        if (executionContext.UserInfo.Id == command.Id) return Result<UserResponse>.Forbidden("You cannot change your own user role.");

        if (executionContext.UserInfo.Role != nameof(UserRole.Owner))
        {
            return Result<UserResponse>.Forbidden("Only owners are allowed to change the user roles of users.");
        }

        var user = await userRepository.GetByIdAsync(command.Id, cancellationToken);
        if (user is null) return Result<UserResponse>.NotFound($"User with id '{command.Id}' not found.");

        var fromUserRole = user.Role;
        user.ChangeUserRole(command.UserRole);
        userRepository.Update(user);

        events.CollectEvent(new UserRoleChanged(user.Id, fromUserRole, command.UserRole));

        return UserResponse.FromUser(user);
    }
}
