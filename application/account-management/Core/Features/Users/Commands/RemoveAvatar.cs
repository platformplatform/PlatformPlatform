using JetBrains.Annotations;
using PlatformPlatform.AccountManagement.Features.Users.Domain;
using PlatformPlatform.SharedKernel.Cqrs;
using PlatformPlatform.SharedKernel.Telemetry;

namespace PlatformPlatform.AccountManagement.Features.Users.Commands;

[PublicAPI]
public sealed record RemoveAvatarCommand : ICommand, IRequest<Result>;

public sealed class RemoveAvatarHandler(IUserRepository userRepository, ITelemetryEventsCollector events)
    : IRequestHandler<RemoveAvatarCommand, Result>
{
    public async Task<Result> Handle(RemoveAvatarCommand command, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetLoggedInUserAsync(cancellationToken);

        user.RemoveAvatar();
        userRepository.Update(user);

        events.CollectEvent(new UserAvatarRemoved());

        return Result.Success();
    }
}
