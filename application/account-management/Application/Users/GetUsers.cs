using Mapster;
using PlatformPlatform.SharedKernel.ApplicationCore.Cqrs;
using PlatformPlatform.SharedKernel.DomainCore.Persistence;

namespace PlatformPlatform.AccountManagement.Application.Users;

public sealed record GetUsersQuery(
    string? Search = null,
    UserRole? UserRole = null,
    SortableUserProperties OrderBy = SortableUserProperties.Name,
    SortOrder SortOrder = SortOrder.Ascending,
    int? PageSize = null,
    int? PageOffset = null
) : IRequest<Result<GetUsersResponseDto>>;

public sealed class GetUsersHandler(IUserRepository userRepository)
    : IRequestHandler<GetUsersQuery, Result<GetUsersResponseDto>>
{
    public async Task<Result<GetUsersResponseDto>> Handle(GetUsersQuery query, CancellationToken cancellationToken)
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
        
        var userResponseDtos = users.Adapt<UserResponseDto[]>();
        return new GetUsersResponseDto(count, totalPages, query.PageOffset ?? 0, userResponseDtos);
    }
}
