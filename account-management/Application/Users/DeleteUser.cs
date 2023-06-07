using JetBrains.Annotations;
using MediatR;
using PlatformPlatform.AccountManagement.Domain.Users;
using PlatformPlatform.SharedKernel.ApplicationCore.Cqrs;

namespace PlatformPlatform.AccountManagement.Application.Users;

public static class DeleteUser
{
    public sealed record Command(UserId Id) : ICommand, IRequest<Result>;

    [UsedImplicitly]
    public sealed class Handler : IRequestHandler<Command, Result>
    {
        private readonly IUserRepository _userRepository;

        public Handler(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public async Task<Result> Handle(Command command, CancellationToken cancellationToken)
        {
            var user = await _userRepository.GetByIdAsync(command.Id, cancellationToken);
            if (user is null)
            {
                return Result.NotFound($"User with id '{command.Id}' not found.");
            }

            _userRepository.Remove(user);
            return Result.NoContent();
        }
    }
}