using JetBrains.Annotations;
using PlatformPlatform.AccountManagement.Features.Users.Domain;
using PlatformPlatform.SharedKernel.Cqrs;
using PlatformPlatform.SharedKernel.Domain;
using PlatformPlatform.SharedKernel.ExecutionContext;
using PlatformPlatform.SharedKernel.Telemetry;

namespace PlatformPlatform.AccountManagement.Features.Users.Commands;

[PublicAPI]
public sealed record DeleteUserCommand(UserId Id) : ICommand, IRequest<Result>;

public sealed class DeleteUserHandler(IUserRepository userRepository, IExecutionContext executionContext, ITelemetryEventsCollector events)
    : IRequestHandler<DeleteUserCommand, Result>
{
    public async Task<Result> Handle(DeleteUserCommand command, CancellationToken cancellationToken)
    {
        if (executionContext.UserInfo.Id == command.Id) return Result.Forbidden("You cannot delete yourself.");

        if (executionContext.UserInfo.Role != UserRole.Owner.ToString())
        {
            return Result.Forbidden("Only owners are allowed to delete other users.");
        }

        var user = await userRepository.GetByIdAsync(command.Id, cancellationToken);
        if (user is null) return Result.NotFound($"User with id '{command.Id}' not found.");

        userRepository.Remove(user);

        events.CollectEvent(new UserDeleted(user.Id));

        return Result.Success();
    }
}
