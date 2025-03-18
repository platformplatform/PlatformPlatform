using FluentValidation;
using PlatformPlatform.AccountManagement.Features.Tenants.Domain;
using PlatformPlatform.AccountManagement.Features.Users.Domain;
using PlatformPlatform.AccountManagement.Features.Users.Shared;
using PlatformPlatform.AccountManagement.Integrations.Gravatar;
using PlatformPlatform.SharedKernel.Cqrs;
using PlatformPlatform.SharedKernel.Domain;
using PlatformPlatform.SharedKernel.ExecutionContext;
using PlatformPlatform.SharedKernel.SinglePageApp;
using PlatformPlatform.SharedKernel.Telemetry;
using PlatformPlatform.SharedKernel.Validation;

namespace PlatformPlatform.AccountManagement.Features.Users.Commands;

internal sealed record CreateUserCommand(TenantId TenantId, string Email, UserRole UserRole, bool EmailConfirmed, string? PreferredLocale)
    : ICommand, IRequest<Result<UserId>>
{
    public string Email { get; } = Email.Trim().ToLower();
}

internal sealed class CreateUserValidator : AbstractValidator<CreateUserCommand>
{
    public CreateUserValidator(IUserRepository userRepository, ITenantRepository tenantRepository)
    {
        RuleFor(x => x.Email).NotEmpty().SetValidator(new SharedValidations.Email());

        RuleFor(x => x)
            .MustAsync((x, cancellationToken) => userRepository.IsEmailFreeAsync(x.Email, cancellationToken))
            .WithName("Email")
            .WithMessage(x => $"The email '{x.Email}' is already in use by another user on this tenant.")
            .When(x => !string.IsNullOrEmpty(x.Email));
    }
}

internal sealed class CreateUserHandler(
    IUserRepository userRepository,
    AvatarUpdater avatarUpdater,
    GravatarClient gravatarClient,
    ITelemetryEventsCollector events,
    IExecutionContext executionContext
) : IRequestHandler<CreateUserCommand, Result<UserId>>
{
    public async Task<Result<UserId>> Handle(CreateUserCommand command, CancellationToken cancellationToken)
    {
        if (executionContext.TenantId is not null && executionContext.TenantId != command.TenantId)
        {
            throw new UnreachableException("Only when signing up a new tenant, is the TenantID allowed to different than the current tenant.");
        }

        var locale = SinglePageAppConfiguration.SupportedLocalizations.Contains(command.PreferredLocale)
            ? command.PreferredLocale
            : string.Empty;
        var user = User.Create(command.TenantId, command.Email, command.UserRole, command.EmailConfirmed, locale);

        await userRepository.AddAsync(user, cancellationToken);
        var gravatar = await gravatarClient.GetGravatar(user.Id, user.Email, cancellationToken);
        if (gravatar is not null)
        {
            await avatarUpdater.UpdateAvatar(user, true, gravatar.ContentType, gravatar.Stream, cancellationToken);
            events.CollectEvent(new GravatarUpdated(gravatar.Stream.Length));
        }

        events.CollectEvent(new UserCreated(user.Id, user.Avatar.IsGravatar));

        return user.Id;
    }
}
