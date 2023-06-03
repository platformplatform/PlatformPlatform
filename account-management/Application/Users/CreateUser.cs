using FluentValidation;
using JetBrains.Annotations;
using MediatR;
using PlatformPlatform.AccountManagement.Domain.Tenants;
using PlatformPlatform.AccountManagement.Domain.Users;
using PlatformPlatform.SharedKernel.ApplicationCore.Cqrs;

namespace PlatformPlatform.AccountManagement.Application.Users;

public static class CreateUser
{
    public sealed record Command(TenantId TenantId, string Email, UserRole UserRole)
        : ICommand, IUserValidation, IRequest<Result<User>>;

    [UsedImplicitly]
    public sealed class Handler : IRequestHandler<Command, Result<User>>
    {
        private readonly IUserRepository _userRepository;

        public Handler(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public async Task<Result<User>> Handle(Command command, CancellationToken cancellationToken)
        {
            var user = User.Create(command.TenantId, command.Email, command.UserRole);
            await _userRepository.AddAsync(user, cancellationToken);
            return user;
        }
    }

    [UsedImplicitly]
    public sealed class Validator : UserValidator<Command>
    {
        public Validator(IUserRepository repository)
        {
            RuleFor(x => x)
                .MustAsync(async (x, token) => await repository.IsEmailFreeAsync(x.TenantId, x.Email, token))
                .WithMessage(x => $"The email '{x.Email}' is already in use by another user on this tenant.")
                .When(x => !string.IsNullOrEmpty(x.Email));
        }
    }
}