using Mapster;
using PlatformPlatform.SharedKernel.ApplicationCore.Cqrs;

namespace PlatformPlatform.AccountManagement.Application.Users;

public sealed record GetUserQuery(UserId Id) : IRequest<Result<UserResponseDto>>;

[UsedImplicitly]
public sealed class GetUserHandler : IRequestHandler<GetUserQuery, Result<UserResponseDto>>
{
    private readonly IUserRepository _userRepository;

    public GetUserHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<Result<UserResponseDto>> Handle(GetUserQuery request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.Id, cancellationToken);
        return user?.Adapt<UserResponseDto>()
               ?? Result<UserResponseDto>.NotFound($"User with id '{request.Id}' not found.");
    }
}