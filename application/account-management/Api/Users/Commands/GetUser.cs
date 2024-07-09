using Mapster;
using PlatformPlatform.AccountManagement.Api.Users.Domain;
using PlatformPlatform.SharedKernel.ApplicationCore.Cqrs;

namespace PlatformPlatform.AccountManagement.Api.Users.Commands;

public sealed record GetUserQuery(UserId Id) : IRequest<Result<UserResponseDto>>;

public sealed class GetUserHandler(IUserRepository userRepository)
    : IRequestHandler<GetUserQuery, Result<UserResponseDto>>
{
    public async Task<Result<UserResponseDto>> Handle(GetUserQuery request, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(request.Id, cancellationToken);
        return user?.Adapt<UserResponseDto>() ?? Result<UserResponseDto>.NotFound($"User with id '{request.Id}' not found.");
    }
}
