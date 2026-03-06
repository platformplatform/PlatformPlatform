using Account.Features.Users.Domain;
using Account.Features.Users.Shared;
using JetBrains.Annotations;
using SharedKernel.Cqrs;
using SharedKernel.Telemetry;

namespace Account.Features.Users.Commands;

[PublicAPI]
public sealed record RemoveAvatarCommand : ICommand, IRequest<Result<UserResponse>>;

public sealed class RemoveAvatarHandler(IUserRepository userRepository, ITelemetryEventsCollector events)
    : IRequestHandler<RemoveAvatarCommand, Result<UserResponse>>
{
    public async Task<Result<UserResponse>> Handle(RemoveAvatarCommand command, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetLoggedInUserAsync(cancellationToken);

        user.RemoveAvatar();
        userRepository.Update(user);

        events.CollectEvent(new UserAvatarRemoved());

        return UserResponse.FromUser(user);
    }
}
