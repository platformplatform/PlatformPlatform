using PlatformPlatform.AccountManagement.Application.TelemetryEvents;
using PlatformPlatform.SharedKernel.ApplicationCore.Cqrs;
using PlatformPlatform.SharedKernel.ApplicationCore.Services;
using PlatformPlatform.SharedKernel.ApplicationCore.TelemetryEvents;

namespace PlatformPlatform.AccountManagement.Application.Users;

[UsedImplicitly]
public sealed record UpdateAvatarCommand(UserId Id, Stream FileSteam, string ContentType) : ICommand, IRequest<Result>;

[UsedImplicitly]
public sealed class UpdateAvatarHandler(
    IUserRepository userRepository,
    IBlobStorage blobStorage,
    ITelemetryEventsCollector events
)
    : IRequestHandler<UpdateAvatarCommand, Result>
{
    private const string ContainerName = "avatars";
    private static readonly string PublicUrl = Environment.GetEnvironmentVariable("PUBLIC_URL")!;

    public async Task<Result> Handle(UpdateAvatarCommand command, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(command.Id, cancellationToken);
        if (user is null) return Result.NotFound($"User with id '{command.Id}' not found.");

        var avatarVersion = user.Avatar.Version + 1;
        var blobName = $"{user.Id}-{avatarVersion}.jpg";

        await blobStorage.UploadAsync(ContainerName, blobName, command.ContentType,
            command.FileSteam, cancellationToken);

        var avatarUrl = $"{PublicUrl}/{ContainerName}/{blobName}";
        user.UpdateAvatar(avatarUrl, avatarVersion);

        userRepository.Update(user);

        events.CollectEvent(new UserAvatarUpdated(command.ContentType, command.FileSteam.Length));

        return Result.Success();
    }
}