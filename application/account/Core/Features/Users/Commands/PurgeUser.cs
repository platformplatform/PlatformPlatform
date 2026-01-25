using JetBrains.Annotations;
using PlatformPlatform.Account.Features.Users.Domain;
using PlatformPlatform.SharedKernel.Cqrs;
using PlatformPlatform.SharedKernel.Domain;
using PlatformPlatform.SharedKernel.ExecutionContext;
using PlatformPlatform.SharedKernel.Telemetry;

namespace PlatformPlatform.Account.Features.Users.Commands;

[PublicAPI]
public sealed record PurgeUserCommand(UserId Id) : ICommand, IRequest<Result>;

public sealed class PurgeUserHandler(IUserRepository userRepository, IExecutionContext executionContext, ITelemetryEventsCollector events)
    : IRequestHandler<PurgeUserCommand, Result>
{
    public async Task<Result> Handle(PurgeUserCommand command, CancellationToken cancellationToken)
    {
        if (executionContext.UserInfo.Role is not (nameof(UserRole.Owner) or nameof(UserRole.Admin)))
        {
            return Result.Forbidden("Only owners and admins can permanently delete users.");
        }

        var user = await userRepository.GetDeletedByIdAsync(command.Id, cancellationToken);
        if (user is null)
        {
            return Result.NotFound($"Deleted user with id '{command.Id}' not found.");
        }

        userRepository.PermanentlyRemove(user);

        events.CollectEvent(new UserPurged(user.Id, UserPurgeReason.SingleUserPurge));

        return Result.Success();
    }
}
