using JetBrains.Annotations;
using MediatR;
using PlatformPlatform.AccountManagement.Domain.Users;
using PlatformPlatform.SharedKernel.ApplicationCore.Cqrs;

namespace PlatformPlatform.AccountManagement.Application.Users.Commands;

public static class UpdateUser
{
    public sealed record Command(UserId Id, string Email, UserRole UserRole)
        : ICommand, IUserValidation, IRequest<Result<User>>;

    public sealed class Handler : IRequestHandler<Command, Result<User>>
    {
        private readonly IUserRepository _userRepository;

        public Handler(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public async Task<Result<User>> Handle(Command command, CancellationToken cancellationToken)
        {
            var user = await _userRepository.GetByIdAsync(command.Id, cancellationToken);
            if (user is null)
            {
                return Result<User>.NotFound($"User with id '{command.Id}' not found.");
            }

            user.Update(command.Email, command.UserRole);
            _userRepository.Update(user);
            return user;
        }
    }

    [UsedImplicitly]
    public sealed class Validator : UserValidator<Command>
    {
    }
}