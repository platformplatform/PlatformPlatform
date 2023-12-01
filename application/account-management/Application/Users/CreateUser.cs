using FluentValidation;
using PlatformPlatform.SharedKernel.ApplicationCore.Cqrs;
using PlatformPlatform.SharedKernel.ApplicationCore.Tracking;

namespace PlatformPlatform.AccountManagement.Application.Users;

public sealed record CreateUserCommand(TenantId TenantId, string Email, UserRole UserRole)
    : ICommand, IUserValidation, IRequest<Result<UserId>>;

[UsedImplicitly]
public sealed class CreateUserHandler(IUserRepository userRepository, IAnalyticEventsCollector analyticEventsCollector)
    : IRequestHandler<CreateUserCommand, Result<UserId>>
{
    public async Task<Result<UserId>> Handle(CreateUserCommand command, CancellationToken cancellationToken)
    {
        var user = User.Create(command.TenantId, command.Email, command.UserRole);
        await userRepository.AddAsync(user, cancellationToken);

        analyticEventsCollector.CollectEvent(
            "UserCreated",
            new Dictionary<string, string> { { "Tenant_Id", command.TenantId.ToString() } }
        );

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