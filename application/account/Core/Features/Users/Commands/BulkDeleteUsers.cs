using Account.Features.Users.Domain;
using Account.Features.Users.Shared;
using FluentValidation;
using JetBrains.Annotations;
using SharedKernel.Cqrs;
using SharedKernel.Domain;
using SharedKernel.ExecutionContext;
using SharedKernel.Telemetry;

namespace Account.Features.Users.Commands;

[PublicAPI]
public sealed record BulkDeleteUsersCommand(UserId[] UserIds) : ICommand, IRequest<Result<UserResponse[]>>;

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
) : IRequestHandler<BulkDeleteUsersCommand, Result<UserResponse[]>>
{
    public async Task<Result<UserResponse[]>> Handle(BulkDeleteUsersCommand command, CancellationToken cancellationToken)
    {
        if (executionContext.UserInfo.Role != nameof(UserRole.Owner))
        {
            return Result<UserResponse[]>.Forbidden("Only owners are allowed to delete other users.");
        }

        if (command.UserIds.Contains(executionContext.UserInfo.Id))
        {
            return Result<UserResponse[]>.Forbidden("You cannot delete yourself.");
        }

        var usersToDelete = await userRepository.GetByIdsAsync(command.UserIds, cancellationToken);

        var missingUserIds = command.UserIds.Where(id => !usersToDelete.Select(u => u.Id).Contains(id)).ToArray();
        if (missingUserIds.Length > 0)
        {
            return Result<UserResponse[]>.NotFound($"Users with ids '{string.Join(", ", missingUserIds.Select(id => id.ToString()))}' not found.");
        }

        userRepository.RemoveRange(usersToDelete);
        foreach (var user in usersToDelete)
        {
            events.CollectEvent(new UserDeleted(user.Id, true));
        }

        events.CollectEvent(new UsersBulkDeleted(command.UserIds.Length));

        return usersToDelete.Select(UserResponse.FromUser).ToArray();
    }
}
