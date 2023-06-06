using JetBrains.Annotations;
using Mapster;
using MediatR;
using PlatformPlatform.AccountManagement.Domain.Users;
using PlatformPlatform.SharedKernel.ApplicationCore.Cqrs;

namespace PlatformPlatform.AccountManagement.Application.Users;

public static class GetUser
{
    public sealed record Query(UserId Id) : IRequest<Result<UserResponseDto>>;

    [UsedImplicitly]
    public sealed class Handler : IRequestHandler<Query, Result<UserResponseDto>>
    {
        private readonly IUserRepository _userRepository;

        public Handler(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public async Task<Result<UserResponseDto>> Handle(Query request, CancellationToken cancellationToken)
        {
            var user = await _userRepository.GetByIdAsync(request.Id, cancellationToken);
            return user?.Adapt<UserResponseDto>()
                   ?? Result<UserResponseDto>.NotFound($"User with id '{request.Id}' not found.");
        }
    }
}