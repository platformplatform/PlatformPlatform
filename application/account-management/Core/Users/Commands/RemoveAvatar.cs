using JetBrains.Annotations;
using PlatformPlatform.AccountManagement.TelemetryEvents;
using PlatformPlatform.AccountManagement.Users.Domain;
using PlatformPlatform.SharedKernel.Cqrs;
using PlatformPlatform.SharedKernel.TelemetryEvents;

namespace PlatformPlatform.AccountManagement.Users.Commands;

[PublicAPI]
public sealed record RemoveAvatarCommand(UserId Id) : ICommand, IRequest<Result>;

public sealed class RemoveAvatarCommandHandler(IUserRepository userRepository, ITelemetryEventsCollector events)
    : IRequestHandler<RemoveAvatarCommand, Result>
{
    public async Task<Result> Handle(RemoveAvatarCommand command, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(command.Id, cancellationToken);
        if (user is null) return Result.NotFound($"User with id '{command.Id}' not found.");

        user.RemoveAvatar();
        userRepository.Update(user);

        events.CollectEvent(new UserAvatarRemoved());

        return Result.Success();
    }
}
