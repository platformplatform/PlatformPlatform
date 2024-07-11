using Mapster;
using PlatformPlatform.AccountManagement.Api.Users.Domain;
using PlatformPlatform.SharedKernel.ApiCore.ApiResults;
using PlatformPlatform.SharedKernel.ApiCore.Endpoints;
using PlatformPlatform.SharedKernel.Application.Cqrs;
using PlatformPlatform.SharedKernel.Domain.Persistence;

namespace PlatformPlatform.AccountManagement.Api.Users.Queries;

public sealed record GetUsersQuery(
    string? Search = null,
    UserRole? UserRole = null,
    SortableUserProperties OrderBy = SortableUserProperties.Name,
    SortOrder SortOrder = SortOrder.Ascending,
    int? PageSize = null,
    int? PageOffset = null
) : IRequest<Result<GetUsersResponseDto>>;

public sealed record GetUsersResponseDto(int TotalCount, int TotalPages, int CurrentPageOffset, UserResponseDto[] Users);

public sealed class GetUsersEndpoint : IEndpoints
{
    public void MapEndpoints(IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/account-management/users").WithTags("Users");

        group.MapGet("/", async Task<ApiResult<GetUsersResponseDto>> ([AsParameters] GetUsersQuery query, ISender mediator)
            => await mediator.Send(query)
        ).Produces<GetUsersResponseDto>();
    }
}

public sealed class GetUsersHandler(UserRepository userRepository)
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
