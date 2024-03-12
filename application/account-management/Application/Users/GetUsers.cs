using Mapster;
using PlatformPlatform.SharedKernel.ApplicationCore.Cqrs;
using PlatformPlatform.SharedKernel.DomainCore.Persistence;

namespace PlatformPlatform.AccountManagement.Application.Users;

[UsedImplicitly]
public sealed record GetUsersQuery(
    string? Search = null,
    UserRole? UserRole = null,
    SortableUserProperties OrderBy = SortableUserProperties.Name,
    SortOrder SortOrder = SortOrder.Ascending,
    int? PageSize = null,
    int? PageOffset = null
)
    : IRequest<Result<UserResponseDto[]>>;

[UsedImplicitly]
public sealed class GetUsersHandler(IUserRepository userRepository)
    : IRequestHandler<GetUsersQuery, Result<UserResponseDto[]>>
{
    public async Task<Result<UserResponseDto[]>> Handle(GetUsersQuery query, CancellationToken cancellationToken)
    {
        var users = await userRepository.Search(
            query.Search,
            query.UserRole,
            query.OrderBy,
            query.SortOrder,
            query.PageSize,
            query.PageOffset,
            cancellationToken
        );
        return users.Select(u => u.Adapt<UserResponseDto>()).ToArray();
    }
}