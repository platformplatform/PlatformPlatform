using FluentValidation;
using JetBrains.Annotations;
using Mapster;
using PlatformPlatform.AccountManagement.Features.Users.Domain;
using PlatformPlatform.SharedKernel.Cqrs;
using PlatformPlatform.SharedKernel.Domain;
using PlatformPlatform.SharedKernel.ExecutionContext;

namespace PlatformPlatform.AccountManagement.Features.Users.Queries;

[PublicAPI]
public sealed record GetDeletedUsersQuery(int? PageOffset = null, int PageSize = 25) : IRequest<Result<DeletedUsersResponse>>;

[PublicAPI]
public sealed record DeletedUsersResponse(int TotalCount, int PageSize, int TotalPages, int CurrentPageOffset, DeletedUserDetails[] Users);

[PublicAPI]
public sealed record DeletedUserDetails(
    UserId Id,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ModifiedAt,
    DateTimeOffset? DeletedAt,
    string Email,
    UserRole Role,
    string FirstName,
    string LastName,
    string Title,
    bool EmailConfirmed,
    string? AvatarUrl
);

public sealed class GetDeletedUsersQueryValidator : AbstractValidator<GetDeletedUsersQuery>
{
    public GetDeletedUsersQueryValidator()
    {
        RuleFor(x => x.PageSize).InclusiveBetween(0, 1000).WithMessage("Page size must be between 0 and 1000.");
        RuleFor(x => x.PageOffset).GreaterThanOrEqualTo(0).WithMessage("Page offset must be greater than or equal to 0.");
    }
}

public sealed class GetDeletedUsersHandler(IUserRepository userRepository, IExecutionContext executionContext)
    : IRequestHandler<GetDeletedUsersQuery, Result<DeletedUsersResponse>>
{
    public async Task<Result<DeletedUsersResponse>> Handle(GetDeletedUsersQuery query, CancellationToken cancellationToken)
    {
        if (executionContext.UserInfo.Role is not (nameof(UserRole.Owner) or nameof(UserRole.Admin)))
        {
            return Result<DeletedUsersResponse>.Forbidden("Only owners and admins can view deleted users.");
        }

        var allDeletedUsers = await userRepository.GetAllDeletedAsync(cancellationToken);

        var totalCount = allDeletedUsers.Length;
        var pageSize = query.PageSize;
        var totalPages = totalCount == 0 ? 1 : (totalCount - 1) / pageSize + 1;
        var pageOffset = query.PageOffset ?? 0;

        if (pageOffset >= totalPages && totalCount > 0)
        {
            return Result<DeletedUsersResponse>.BadRequest($"The page offset '{pageOffset}' is greater than the total number of pages.");
        }

        var pagedUsers = allDeletedUsers
            .OrderByDescending(u => u.DeletedAt)
            .Skip(pageOffset * pageSize)
            .Take(pageSize)
            .ToArray();

        var userResponses = pagedUsers.Adapt<DeletedUserDetails[]>();
        return new DeletedUsersResponse(totalCount, pageSize, totalPages, pageOffset, userResponses);
    }
}
