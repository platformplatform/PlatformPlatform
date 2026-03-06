using Account.Features.Users.Domain;
using Account.Features.Users.Shared;
using FluentValidation;
using JetBrains.Annotations;
using SharedKernel.Cqrs;
using SharedKernel.Telemetry;

namespace Account.Features.Users.Commands;

[PublicAPI]
public sealed record UpdateAvatarCommand(Stream FileSteam, string ContentType) : ICommand, IRequest<Result<UserResponse>>;

public sealed class UpdateAvatarValidator : AbstractValidator<UpdateAvatarCommand>
{
    public UpdateAvatarValidator()
    {
        RuleFor(x => x.ContentType)
            .Must(x => x is "image/jpeg" or "image/png" or "image/gif" or "image/webp") // Align with frontend
            .WithMessage(_ => "Image must be of type JPEG, PNG, GIF, or WebP.");

        RuleFor(x => x.FileSteam.Length)
            .LessThanOrEqualTo(1024 * 1024)
            .WithMessage(_ => "Image must be smaller than 1 MB");
    }
}

public sealed class UpdateAvatarHandler(IUserRepository userRepository, AvatarUpdater avatarUpdater, ITelemetryEventsCollector events)
    : IRequestHandler<UpdateAvatarCommand, Result<UserResponse>>
{
    public async Task<Result<UserResponse>> Handle(UpdateAvatarCommand command, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetLoggedInUserAsync(cancellationToken);

        if (await avatarUpdater.UpdateAvatar(user, false, command.ContentType, command.FileSteam, cancellationToken))
        {
            events.CollectEvent(new UserAvatarUpdated(command.ContentType, command.FileSteam.Length));
        }

        return UserResponse.FromUser(user);
    }
}
