using Account.Features.Users.Domain;
using Account.Features.Users.Shared;
using JetBrains.Annotations;
using SharedKernel.Cqrs;
using SharedKernel.Domain;
using SharedKernel.ExecutionContext;
using SharedKernel.Telemetry;

namespace Account.Features.Users.Commands;

[PublicAPI]
public sealed record DeleteUserCommand(UserId Id) : ICommand, IRequest<Result<UserResponse>>;

public sealed class DeleteUserHandler(IUserRepository userRepository, IExecutionContext executionContext, ITelemetryEventsCollector events)
    : IRequestHandler<DeleteUserCommand, Result<UserResponse>>
{
    public async Task<Result<UserResponse>> Handle(DeleteUserCommand command, CancellationToken cancellationToken)
    {
        if (executionContext.UserInfo.Id == command.Id) return Result<UserResponse>.Forbidden("You cannot delete yourself.");

        if (executionContext.UserInfo.Role != nameof(UserRole.Owner))
        {
            return Result<UserResponse>.Forbidden("Only owners are allowed to delete other users.");
        }

        var user = await userRepository.GetByIdAsync(command.Id, cancellationToken);
        if (user is null) return Result<UserResponse>.NotFound($"User with id '{command.Id}' not found.");

        userRepository.Remove(user);
        events.CollectEvent(new UserDeleted(user.Id));

        return UserResponse.FromUser(user);
    }
}
