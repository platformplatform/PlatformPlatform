using JetBrains.Annotations;
using Mapster;
using PlatformPlatform.AccountManagement.Users.Domain;
using PlatformPlatform.SharedKernel.Cqrs;

namespace PlatformPlatform.AccountManagement.Users.Queries;

[PublicAPI]
public sealed record GetUserQuery(UserId Id) : IRequest<Result<UserResponseDto>>;

[PublicAPI]
public sealed record UserResponseDto(
    string Id,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ModifiedAt,
    string Email,
    UserRole Role,
    string FirstName,
    string LastName,
    string Title,
    bool EmailConfirmed,
    string? AvatarUrl
);

public sealed class GetUserHandler(IUserRepository userRepository)
    : IRequestHandler<GetUserQuery, Result<UserResponseDto>>
{
    public async Task<Result<UserResponseDto>> Handle(GetUserQuery request, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(request.Id, cancellationToken);
        return user?.Adapt<UserResponseDto>() ?? Result<UserResponseDto>.NotFound($"User with id '{request.Id}' not found.");
    }
}
