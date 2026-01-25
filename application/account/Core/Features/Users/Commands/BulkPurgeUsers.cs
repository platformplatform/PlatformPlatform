using FluentValidation;
using JetBrains.Annotations;
using PlatformPlatform.Account.Features.Users.Domain;
using PlatformPlatform.SharedKernel.Cqrs;
using PlatformPlatform.SharedKernel.Domain;
using PlatformPlatform.SharedKernel.ExecutionContext;
using PlatformPlatform.SharedKernel.Telemetry;

namespace PlatformPlatform.Account.Features.Users.Commands;

[PublicAPI]
public sealed record BulkPurgeUsersCommand(UserId[] UserIds) : ICommand, IRequest<Result>;

public sealed class BulkPurgeUsersValidator : AbstractValidator<BulkPurgeUsersCommand>
{
    public BulkPurgeUsersValidator()
    {
        RuleFor(x => x.UserIds)
            .NotEmpty()
            .WithMessage("At least one user must be selected for deletion.")
            .Must(ids => ids.Length <= 100)
            .WithMessage("Cannot delete more than 100 users at once.");
    }
}

public sealed class BulkPurgeUsersHandler(IUserRepository userRepository, IExecutionContext executionContext, ITelemetryEventsCollector events)
    : IRequestHandler<BulkPurgeUsersCommand, Result>
{
    public async Task<Result> Handle(BulkPurgeUsersCommand command, CancellationToken cancellationToken)
    {
        if (executionContext.UserInfo.Role != nameof(UserRole.Owner))
        {
            return Result.Forbidden("Only owners can permanently delete users from the recycle bin.");
        }

        var deletedUsers = await userRepository.GetDeletedByIdsAsync(command.UserIds, cancellationToken);

        var missingUserIds = command.UserIds.Where(id => !deletedUsers.Select(u => u.Id).Contains(id)).ToArray();
        if (missingUserIds.Length > 0)
        {
            return Result.NotFound($"Deleted users with ids '{string.Join(", ", missingUserIds.Select(id => id.ToString()))}' not found.");
        }

        userRepository.PermanentlyRemoveRange(deletedUsers);

        foreach (var user in deletedUsers)
        {
            events.CollectEvent(new UserPurged(user.Id, UserPurgeReason.BulkUserPurge));
        }

        return Result.Success();
    }
}
