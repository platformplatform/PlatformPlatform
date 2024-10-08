using FluentValidation;
using JetBrains.Annotations;
using Mapster;
using PlatformPlatform.AccountManagement.Users.Domain;
using PlatformPlatform.SharedKernel.Cqrs;
using PlatformPlatform.SharedKernel.Persistence;

namespace PlatformPlatform.AccountManagement.Users.Queries;

[PublicAPI]
public sealed record GetUsersQuery(
    string? Search = null,
    UserRole? UserRole = null,
    SortableUserProperties OrderBy = SortableUserProperties.Name,
    SortOrder SortOrder = SortOrder.Ascending,
    int PageSize = 25,
    int? PageOffset = null
) : IRequest<Result<GetUsersResponseDto>>;

[PublicAPI]
public sealed record GetUsersResponseDto(int TotalCount, int PageSize, int TotalPages, int CurrentPageOffset, UsersResponseUserDto[] Users);

[PublicAPI]
public sealed record UsersResponseUserDto(
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

public sealed class GetUsersQueryValidator : AbstractValidator<GetUsersQuery>
{
    public GetUsersQueryValidator()
    {
        RuleFor(x => x.Search).MaximumLength(100).WithMessage("The search term must be at most 100 characters.");
        RuleFor(x => x.PageSize).InclusiveBetween(0, 1000).WithMessage("The page size must be between 0 and 1000.");
        RuleFor(x => x.PageOffset).GreaterThanOrEqualTo(0).WithMessage("The page offset must be greater than or equal to 0.");
    }
}

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

        if (query.PageOffset.HasValue && query.PageOffset.Value >= totalPages)
        {
            return Result<GetUsersResponseDto>.BadRequest($"The page offset {query.PageOffset.Value} is greater than the total number of pages.");
        }

        var userResponseDtos = users.Adapt<UsersResponseUserDto[]>();
        return new GetUsersResponseDto(count, query.PageSize, totalPages, query.PageOffset ?? 0, userResponseDtos);
    }
}
