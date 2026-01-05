using JetBrains.Annotations;
using PlatformPlatform.AccountManagement.Features.Users.Domain;
using PlatformPlatform.SharedKernel.Cqrs;
using PlatformPlatform.SharedKernel.Domain;
using PlatformPlatform.SharedKernel.ExecutionContext;
using PlatformPlatform.SharedKernel.Telemetry;

namespace PlatformPlatform.AccountManagement.Features.Users.Commands;

[PublicAPI]
public sealed record RestoreUserCommand(UserId Id) : ICommand, IRequest<Result>;

public sealed class RestoreUserHandler(IUserRepository userRepository, IExecutionContext executionContext, ITelemetryEventsCollector events)
    : IRequestHandler<RestoreUserCommand, Result>
{
    public async Task<Result> Handle(RestoreUserCommand command, CancellationToken cancellationToken)
    {
        if (executionContext.UserInfo.Role is not (nameof(UserRole.Owner) or nameof(UserRole.Admin)))
        {
            return Result.Forbidden("Only owners and admins can restore deleted users.");
        }

        var user = await userRepository.GetDeletedByIdAsync(command.Id, cancellationToken);
        if (user is null)
        {
            return Result.NotFound($"Deleted user with id '{command.Id}' not found.");
        }

        userRepository.Restore(user);

        events.CollectEvent(new UserRestored(user.Id));

        return Result.Success();
    }
}
