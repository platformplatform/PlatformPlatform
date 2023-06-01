using FluentValidation;
using JetBrains.Annotations;
using MediatR;
using PlatformPlatform.AccountManagement.Domain.Users;
using PlatformPlatform.SharedKernel.ApplicationCore.Cqrs;

namespace PlatformPlatform.AccountManagement.Application.Users;

public static class CreateUser
{
    public sealed record Command(string Email, UserRole UserRole)
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
            var user = User.Create(command.Email, command.UserRole);
            await _userRepository.AddAsync(user, cancellationToken);
            return user;
        }
    }

    [UsedImplicitly]
    public sealed class Validator : UserValidator<Command>
    {
        public Validator(IUserRepository repository)
        {
            RuleFor(x => x.Email)
                .MustAsync(async (email, token) => await repository.IsEmailFreeAsync(email, token))
                .WithMessage(x => $"The email '{x.Email}' is already in use by another user.")
                .When(x => !string.IsNullOrEmpty(x.Email));
        }
    }
}