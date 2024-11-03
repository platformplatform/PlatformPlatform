using JetBrains.Annotations;
using Mapster;
using PlatformPlatform.AccountManagement.Features.Users.Domain;
using PlatformPlatform.SharedKernel.Cqrs;
using PlatformPlatform.SharedKernel.Domain;

namespace PlatformPlatform.AccountManagement.Features.Users.Queries;

[PublicAPI]
public sealed record GetUserQuery(UserId Id) : IRequest<Result<UserResponse>>;

[PublicAPI]
public sealed record UserResponse(
    UserId Id,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ModifiedAt,
    string Email,
    UserRole Role,
    string FirstName,
    string LastName,
    string Title,
    string? AvatarUrl
);

public sealed class GetUserHandler(IUserRepository userRepository)
    : IRequestHandler<GetUserQuery, Result<UserResponse>>
{
    public async Task<Result<UserResponse>> Handle(GetUserQuery request, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(request.Id, cancellationToken);
        return user?.Adapt<UserResponse>() ?? Result<UserResponse>.NotFound($"User with id '{request.Id}' not found.");
    }
}
