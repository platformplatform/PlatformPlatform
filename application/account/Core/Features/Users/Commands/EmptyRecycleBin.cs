using JetBrains.Annotations;
using PlatformPlatform.Account.Features.Users.Domain;
using PlatformPlatform.SharedKernel.Cqrs;
using PlatformPlatform.SharedKernel.ExecutionContext;
using PlatformPlatform.SharedKernel.Telemetry;

namespace PlatformPlatform.Account.Features.Users.Commands;

[PublicAPI]
public sealed record EmptyRecycleBinCommand : ICommand, IRequest<Result<int>>;

public sealed class EmptyRecycleBinHandler(IUserRepository userRepository, IExecutionContext executionContext, ITelemetryEventsCollector events)
    : IRequestHandler<EmptyRecycleBinCommand, Result<int>>
{
    public async Task<Result<int>> Handle(EmptyRecycleBinCommand command, CancellationToken cancellationToken)
    {
        if (executionContext.UserInfo.Role != nameof(UserRole.Owner))
        {
            return Result<int>.Forbidden("Only owners can empty the deleted users recycle bin.");
        }

        var deletedUsers = await userRepository.GetAllDeletedAsync(cancellationToken);

        if (deletedUsers.Length == 0)
        {
            return 0;
        }

        userRepository.PermanentlyRemoveRange(deletedUsers);

        foreach (var user in deletedUsers)
        {
            events.CollectEvent(new UserPurged(user.Id, UserPurgeReason.RecycleBinPurge));
        }

        return deletedUsers.Length;
    }
}
