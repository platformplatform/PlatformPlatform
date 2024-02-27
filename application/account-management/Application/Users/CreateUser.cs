using FluentValidation;
using PlatformPlatform.AccountManagement.Application.TelemetryEvents;
using PlatformPlatform.SharedKernel.ApplicationCore.Cqrs;
using PlatformPlatform.SharedKernel.ApplicationCore.TelemetryEvents;

namespace PlatformPlatform.AccountManagement.Application.Users;

public sealed record CreateUserCommand(
    TenantId TenantId,
    string Email,
    string FirstName,
    string LastName,
    UserRole UserRole,
    bool EmailConfirmed
)
    : ICommand, IUserValidation, IRequest<Result<UserId>>;

[UsedImplicitly]
public sealed class CreateUserHandler(IUserRepository userRepository, ITelemetryEventsCollector events)
    : IRequestHandler<CreateUserCommand, Result<UserId>>
{
    public async Task<Result<UserId>> Handle(CreateUserCommand command, CancellationToken cancellationToken)
    {
        var user = User.Create(
            command.TenantId,
            command.Email,
            command.FirstName,
            command.LastName,
            command.UserRole,
            command.EmailConfirmed
        );
        await userRepository.AddAsync(user, cancellationToken);

        events.CollectEvent(new UserCreated(command.TenantId));

        return user.Id;
    }
}

[UsedImplicitly]
public sealed class CreateUserValidator : UserValidator<CreateUserCommand>
{
    public CreateUserValidator(IUserRepository userRepository, ITenantRepository tenantRepository)
    {
        RuleFor(x => x.TenantId)
            .MustAsync(tenantRepository.ExistsAsync)
            .WithMessage(x => $"The tenant '{x.TenantId}' does not exist.")
            .When(x => !string.IsNullOrEmpty(x.Email));

        RuleFor(x => x)
            .MustAsync((x, cancellationToken)
                => userRepository.IsEmailFreeAsync(x.TenantId, x.Email, cancellationToken))
            .WithName("Email")
            .WithMessage(x => $"The email '{x.Email}' is already in use by another user on this tenant.")
            .When(x => !string.IsNullOrEmpty(x.Email));
    }
}