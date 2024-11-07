using FluentValidation;
using JetBrains.Annotations;
using PlatformPlatform.AccountManagement.Features.Tenants.Domain;
using PlatformPlatform.AccountManagement.Features.Users.Avatars;
using PlatformPlatform.AccountManagement.Features.Users.Domain;
using PlatformPlatform.AccountManagement.Integrations.Gravatar;
using PlatformPlatform.AccountManagement.TelemetryEvents;
using PlatformPlatform.SharedKernel.Cqrs;
using PlatformPlatform.SharedKernel.Domain;
using PlatformPlatform.SharedKernel.TelemetryEvents;
using PlatformPlatform.SharedKernel.Validation;

namespace PlatformPlatform.AccountManagement.Features.Users.Commands;

[PublicAPI]
public sealed record CreateUserCommand(TenantId TenantId, string Email, UserRole UserRole, bool EmailConfirmed)
    : ICommand, IRequest<Result<UserId>>;

public sealed class CreateUserValidator : AbstractValidator<CreateUserCommand>
{
    public CreateUserValidator(IUserRepository userRepository, ITenantRepository tenantRepository)
    {
        RuleFor(x => x.Email).NotEmpty().SetValidator(new SharedValidations.Email());

        RuleFor(x => x.TenantId)
            .MustAsync((x, cancellationToken) => tenantRepository.ExistsAsync(new TenantId(x), cancellationToken))
            .WithMessage(x => $"The tenant '{x.TenantId}' does not exist.")
            .When(x => !string.IsNullOrEmpty(x.Email));

        RuleFor(x => x)
            .MustAsync((x, cancellationToken) => userRepository.IsEmailFreeAsync(x.Email, cancellationToken))
            .WithName("Email")
            .WithMessage(x => $"The email '{x.Email}' is already in use by another user on this tenant.")
            .When(x => !string.IsNullOrEmpty(x.Email));
    }
}

public sealed class CreateUserHandler(
    IUserRepository userRepository,
    AvatarUpdater avatarUpdater,
    GravatarClient gravatarClient,
    ITelemetryEventsCollector events
) : IRequestHandler<CreateUserCommand, Result<UserId>>
{
    public async Task<Result<UserId>> Handle(CreateUserCommand command, CancellationToken cancellationToken)
    {
        var user = User.Create(command.TenantId, command.Email, command.UserRole, command.EmailConfirmed);

        await userRepository.AddAsync(user, cancellationToken);
        var gravatar = await gravatarClient.GetGravatar(user.Id, user.Email, cancellationToken);
        if (gravatar is not null)
        {
            await avatarUpdater.UpdateAvatar(user, true, gravatar.ContentType, gravatar.Stream, cancellationToken);
            events.CollectEvent(new GravatarUpdated(user.Id, gravatar.Stream.Length));
        }

        events.CollectEvent(new UserCreated(user.TenantId, user.Avatar.IsGravatar));

        return user.Id;
    }
}
