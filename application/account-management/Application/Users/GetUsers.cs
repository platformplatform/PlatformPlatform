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
    : IRequest<Result<SearchUsersDto>>;

[UsedImplicitly]
public sealed class GetUsersHandler(IUserRepository userRepository)
    : IRequestHandler<GetUsersQuery, Result<SearchUsersDto>>
{
    public async Task<Result<SearchUsersDto>> Handle(GetUsersQuery query, CancellationToken cancellationToken)
    {
        var (users, count, totalPages) = await userRepository.Search(
            query.Search,
            query.UserRole,
            query.OrderBy,
            query.SortOrder,
            query.PageSize,
            query.PageOffset,
            cancellationToken
        );

        var userResponseDtos = users.Select(u => u.Adapt<UserResponseDto>()).ToArray();
        return new SearchUsersDto(count, totalPages, query.PageOffset ?? 0, userResponseDtos);
    }
}