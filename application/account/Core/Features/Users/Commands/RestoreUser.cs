using Account.Features.Users.Domain;
using Account.Features.Users.Shared;
using JetBrains.Annotations;
using SharedKernel.Cqrs;
using SharedKernel.Domain;
using SharedKernel.ExecutionContext;
using SharedKernel.Telemetry;

namespace Account.Features.Users.Commands;

[PublicAPI]
public sealed record RestoreUserCommand(UserId Id) : ICommand, IRequest<Result<UserResponse>>;

public sealed class RestoreUserHandler(IUserRepository userRepository, IExecutionContext executionContext, ITelemetryEventsCollector events)
    : IRequestHandler<RestoreUserCommand, Result<UserResponse>>
{
    public async Task<Result<UserResponse>> Handle(RestoreUserCommand command, CancellationToken cancellationToken)
    {
        if (executionContext.UserInfo.Role is not (nameof(UserRole.Owner) or nameof(UserRole.Admin)))
        {
            return Result<UserResponse>.Forbidden("Only owners and admins can restore deleted users.");
        }

        var user = await userRepository.GetDeletedByIdAsync(command.Id, cancellationToken);
        if (user is null)
        {
            return Result<UserResponse>.NotFound($"Deleted user with id '{command.Id}' not found.");
        }

        userRepository.Restore(user);

        events.CollectEvent(new UserRestored(user.Id));

        return UserResponse.FromUser(user);
    }
}
