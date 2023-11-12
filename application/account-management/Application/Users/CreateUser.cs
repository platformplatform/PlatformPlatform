using FluentValidation;
using PlatformPlatform.SharedKernel.ApplicationCore.Cqrs;

namespace PlatformPlatform.AccountManagement.Application.Users;

public sealed record CreateUserCommand(TenantId TenantId, string Email, UserRole UserRole)
    : ICommand, IUserValidation, IRequest<Result<UserId>>;

[UsedImplicitly]
public sealed class CreateUserHandler : IRequestHandler<CreateUserCommand, Result<UserId>>
{
    private readonly IUserRepository _userRepository;

    public CreateUserHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<Result<UserId>> Handle(CreateUserCommand command, CancellationToken cancellationToken)
    {
        var user = User.Create(command.TenantId, command.Email, command.UserRole);
        await _userRepository.AddAsync(user, cancellationToken);
        return user.Id;
    }
}

[UsedImplicitly]
public sealed class CreateUserValidator : UserValidator<CreateUserCommand>
{
    public CreateUserValidator(IUserRepository userRepository, ITenantRepository tenantRepository)
    {
        RuleFor(x => x.TenantId)
            .MustAsync(async (tenantId, cancellationToken) =>
                await tenantRepository.ExistsAsync(tenantId, cancellationToken))
            .WithMessage(x => $"The tenant '{x.TenantId}' does not exist.")
            .When(x => !string.IsNullOrEmpty(x.Email));

        RuleFor(x => x)
            .MustAsync(async (x, cancellationToken)
                => await userRepository.IsEmailFreeAsync(x.TenantId, x.Email, cancellationToken))
            .WithName("Email")
            .WithMessage(x => $"The email '{x.Email}' is already in use by another user on this tenant.")
            .When(x => !string.IsNullOrEmpty(x.Email));
    }
}