using FluentValidation;
using JetBrains.Annotations;
using PlatformPlatform.AccountManagement.Features.Users.Avatars;
using PlatformPlatform.AccountManagement.Features.Users.Domain;
using PlatformPlatform.AccountManagement.TelemetryEvents;
using PlatformPlatform.SharedKernel.Cqrs;
using PlatformPlatform.SharedKernel.TelemetryEvents;

namespace PlatformPlatform.AccountManagement.Features.Users.Commands;

[PublicAPI]
public sealed record UpdateAvatarCommand(Stream FileSteam, string ContentType) : ICommand, IRequest<Result>;

public sealed class UpdateAvatarValidator : AbstractValidator<UpdateAvatarCommand>
{
    public UpdateAvatarValidator()
    {
        RuleFor(x => x.ContentType)
            .Must(x => x == "image/jpeg")
            .WithMessage(_ => "Image must be of type Jpeg.");

        RuleFor(x => x.FileSteam.Length)
            .LessThanOrEqualTo(1024 * 1024)
            .WithMessage(_ => "Image must be less than 1MB.");
    }
}

public sealed class UpdateAvatarHandler(
    IUserRepository userRepository,
    AvatarUpdater avatarUpdater,
    ITelemetryEventsCollector events
) : IRequestHandler<UpdateAvatarCommand, Result>
{
    public async Task<Result> Handle(UpdateAvatarCommand command, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetLoggedInUserAsync(cancellationToken);
        if (user is null)
        {
            return Result.BadRequest("User not found.");
        }

        if (await avatarUpdater.UpdateAvatar(user, false, command.ContentType, command.FileSteam, cancellationToken))
        {
            events.CollectEvent(new UserAvatarUpdated(command.ContentType, command.FileSteam.Length));
        }

        return Result.Success();
    }
}
