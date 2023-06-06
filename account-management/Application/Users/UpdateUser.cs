using System.Text.Json.Serialization;
using JetBrains.Annotations;
using Mapster;
using MediatR;
using PlatformPlatform.AccountManagement.Domain.Users;
using PlatformPlatform.SharedKernel.ApplicationCore.Cqrs;

namespace PlatformPlatform.AccountManagement.Application.Users;

public static class UpdateUser
{
    public sealed record Command : ICommand, IUserValidation, IRequest<Result<UserResponseDto>>
    {
        [JsonIgnore] // Removes the Id from the API contract
        public UserId Id { get; init; } = null!;

        public required UserRole UserRole { get; init; }

        public required string Email { get; init; }
    }

    [UsedImplicitly]
    public sealed class Handler : IRequestHandler<Command, Result<UserResponseDto>>
    {
        private readonly IUserRepository _userRepository;

        public Handler(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public async Task<Result<UserResponseDto>> Handle(Command command, CancellationToken cancellationToken)
        {
            var user = await _userRepository.GetByIdAsync(command.Id, cancellationToken);
            if (user is null)
            {
                return Result<UserResponseDto>.NotFound($"User with id '{command.Id}' not found.");
            }

            user.Update(command.Email, command.UserRole);
            _userRepository.Update(user);
            return user.Adapt<UserResponseDto>();
        }
    }

    [UsedImplicitly]
    public sealed class Validator : UserValidator<Command>
    {
    }
}