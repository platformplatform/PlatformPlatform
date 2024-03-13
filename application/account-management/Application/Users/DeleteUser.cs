using PlatformPlatform.AccountManagement.Application.TelemetryEvents;
using PlatformPlatform.SharedKernel.ApplicationCore.Cqrs;
using PlatformPlatform.SharedKernel.ApplicationCore.TelemetryEvents;

namespace PlatformPlatform.AccountManagement.Application.Users;

[UsedImplicitly]
public sealed record DeleteUserCommand(UserId Id) : ICommand, IRequest<Result>;

[UsedImplicitly]
public sealed class DeleteUserHandler(IUserRepository userRepository, ITelemetryEventsCollector events)
    : IRequestHandler<DeleteUserCommand, Result>
{
    public async Task<Result> Handle(DeleteUserCommand command, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(command.Id, cancellationToken);
        if (user is null) return Result.NotFound($"User with id '{command.Id}' not found.");

        userRepository.Remove(user);

        events.CollectEvent(new UserDeleted());

        return Result.Success();
    }
}