using FluentValidation;
using JetBrains.Annotations;
using PlatformPlatform.AccountManagement.Features.Users.Domain;
using PlatformPlatform.SharedKernel.Cqrs;
using PlatformPlatform.SharedKernel.Domain;
using PlatformPlatform.SharedKernel.ExecutionContext;
using PlatformPlatform.SharedKernel.Telemetry;

namespace PlatformPlatform.AccountManagement.Features.Users.Commands;

[PublicAPI]
public sealed record BulkDeleteUsersCommand(UserId[] UserIds) : ICommand, IRequest<Result>;

public sealed class BulkDeleteUsersValidator : AbstractValidator<BulkDeleteUsersCommand>
{
    public BulkDeleteUsersValidator()
    {
        RuleFor(x => x.UserIds)
            .NotEmpty()
            .WithMessage("At least one user must be selected for deletion.")
            .Must(ids => ids.Length <= 100)
            .WithMessage("Cannot delete more than 100 users at once.");
    }
}

public sealed class BulkDeleteUsersHandler(
    IUserRepository userRepository,
    IExecutionContext executionContext,
    ITelemetryEventsCollector events
) : IRequestHandler<BulkDeleteUsersCommand, Result>
{
    public async Task<Result> Handle(BulkDeleteUsersCommand command, CancellationToken cancellationToken)
    {
        if (executionContext.UserInfo.Role != nameof(UserRole.Owner))
        {
            return Result.Forbidden("Only owners are allowed to delete other users.");
        }

        if (command.UserIds.Contains(executionContext.UserInfo.Id))
        {
            return Result.Forbidden("You cannot delete yourself.");
        }

        var usersToDelete = await userRepository.GetByIdsAsync(command.UserIds, cancellationToken);

        var missingUserIds = command.UserIds.Where(id => !usersToDelete.Select(u => u.Id).Contains(id)).ToArray();
        if (missingUserIds.Length > 0)
        {
            return Result.NotFound($"Users with ids '{string.Join(", ", missingUserIds.Select(id => id.ToString()))}' not found.");
        }

        var usersToSoftDelete = usersToDelete.Where(u => u.EmailConfirmed).ToArray();
        var usersToHardDelete = usersToDelete.Where(u => !u.EmailConfirmed).ToArray();

        if (usersToSoftDelete.Length > 0)
        {
            userRepository.RemoveRange(usersToSoftDelete);
            foreach (var user in usersToSoftDelete)
            {
                events.CollectEvent(new UserDeleted(user.Id, true));
            }
        }

        if (usersToHardDelete.Length > 0)
        {
            userRepository.PermanentlyRemoveRange(usersToHardDelete);
            foreach (var user in usersToHardDelete)
            {
                events.CollectEvent(new UserPurged(user.Id, UserPurgeReason.NeverActivated));
            }
        }

        events.CollectEvent(new UsersBulkDeleted(command.UserIds.Length));

        return Result.Success();
    }
}
