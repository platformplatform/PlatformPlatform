using JetBrains.Annotations;
using Mapster;
using PlatformPlatform.AccountManagement.Features.Users.Domain;
using PlatformPlatform.SharedKernel.Cqrs;
using PlatformPlatform.SharedKernel.Domain;

namespace PlatformPlatform.AccountManagement.Features.Users.Queries;

[PublicAPI]
public sealed record GetUserQuery : IRequest<Result<CurrentUserResponse>>;

[PublicAPI]
public sealed record CurrentUserResponse(
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
    : IRequestHandler<GetUserQuery, Result<CurrentUserResponse>>
{
    public async Task<Result<CurrentUserResponse>> Handle(GetUserQuery query, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetLoggedInUserAsync(cancellationToken);
        return user.Adapt<CurrentUserResponse>();
    }
}
