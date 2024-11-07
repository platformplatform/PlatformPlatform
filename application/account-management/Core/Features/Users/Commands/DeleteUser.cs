using JetBrains.Annotations;
using PlatformPlatform.AccountManagement.Features.Users.Domain;
using PlatformPlatform.AccountManagement.TelemetryEvents;
using PlatformPlatform.SharedKernel.Cqrs;
using PlatformPlatform.SharedKernel.Domain;
using PlatformPlatform.SharedKernel.TelemetryEvents;

namespace PlatformPlatform.AccountManagement.Features.Users.Commands;

[PublicAPI]
public sealed record DeleteUserCommand(UserId Id) : ICommand, IRequest<Result>;

public sealed class DeleteUserHandler(IUserRepository userRepository, ITelemetryEventsCollector events)
    : IRequestHandler<DeleteUserCommand, Result>
{
    public async Task<Result> Handle(DeleteUserCommand command, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(command.Id, cancellationToken);
        if (user is null) return Result.NotFound($"User with id '{command.Id}' not found.");

        userRepository.Remove(user);

        events.CollectEvent(new UserDeleted(user.Id));

        return Result.Success();
    }
}
