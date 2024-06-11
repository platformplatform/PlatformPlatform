using PlatformPlatform.AccountManagement.Application.TelemetryEvents;
using PlatformPlatform.SharedKernel.ApplicationCore.Cqrs;
using PlatformPlatform.SharedKernel.ApplicationCore.TelemetryEvents;

namespace PlatformPlatform.AccountManagement.Application.Users;

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
