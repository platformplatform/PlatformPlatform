using Account.Features.Tenants.Domain;
using Account.Features.Users.Domain;
using FluentValidation;
using JetBrains.Annotations;
using SharedKernel.Cqrs;
using SharedKernel.Domain;
using SharedKernel.Persistence;

namespace Account.Features.Users.BackOffice.Queries;

[PublicAPI]
public sealed record GetBackOfficeUsersQuery(
    string? Search = null,
    UserRole[]? Roles = null,
    UserActivityFilter? Activity = null,
    SortableBackOfficeUserProperties OrderBy = SortableBackOfficeUserProperties.Name,
    SortOrder SortOrder = SortOrder.Ascending,
    int PageOffset = 0,
    int PageSize = 25
) : IRequest<Result<BackOfficeUsersResponse>>
{
    public string? Search { get; } = Search?.Trim().ToLower();

    public UserRole[] Roles { get; } = Roles ?? [];
}

[PublicAPI]
public sealed record BackOfficeUsersResponse(int TotalCount, int PageSize, int TotalPages, int CurrentPageOffset, BackOfficeUserSummary[] Users);

[PublicAPI]
public sealed record BackOfficeUserSummary(
    UserId Id,
    TenantId TenantId,
    string TenantName,
    string Email,
    string? FirstName,
    string? LastName,
    string? Title,
    UserRole Role,
    bool EmailConfirmed,
    DateTimeOffset CreatedAt,
    DateTimeOffset? LastSeenAt,
    string? AvatarUrl
);

[PublicAPI]
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum UserActivityFilter
{
    ActiveLast24Hours,
    ActiveLast7Days,
    ActiveLast30Days,
    InactiveOver30Days
}

[PublicAPI]
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum SortableBackOfficeUserProperties
{
    Name,
    Email,
    Role,
    LastSeenAt,
    CreatedAt
}

public sealed class GetBackOfficeUsersQueryValidator : AbstractValidator<GetBackOfficeUsersQuery>
{
    public GetBackOfficeUsersQueryValidator()
    {
        // Users page is search-only by design - the table has millions of rows. The frontend renders a "Type to search"
        // empty state until the operator types at least 2 characters, so the API enforces the same minimum.
        RuleFor(x => x.Search).NotEmpty().WithMessage("Search must be between 2 and 100 characters.");
        RuleFor(x => x.Search).MinimumLength(2).MaximumLength(100).WithMessage("Search must be between 2 and 100 characters.");
        RuleFor(x => x.Roles.Length).LessThanOrEqualTo(10).WithMessage("Roles filter must contain no more than 10 values.");
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100).WithMessage("Page size must be between 1 and 100.");
        RuleFor(x => x.PageOffset).GreaterThanOrEqualTo(0).WithMessage("Page offset must be greater than or equal to 0.");
    }
}

public sealed class GetBackOfficeUsersHandler(IUserRepository userRepository, ITenantRepository tenantRepository, TimeProvider timeProvider)
    : IRequestHandler<GetBackOfficeUsersQuery, Result<BackOfficeUsersResponse>>
{
    public async Task<Result<BackOfficeUsersResponse>> Handle(GetBackOfficeUsersQuery query, CancellationToken cancellationToken)
    {
        var (users, totalCount, totalPages) = await userRepository.SearchAllUsersUnfilteredAsync(
            query.Search!,
            query.Roles,
            query.Activity,
            timeProvider.GetUtcNow(),
            query.OrderBy,
            query.SortOrder,
            query.PageOffset,
            query.PageSize,
            cancellationToken
        );

        var tenantIds = users.Select(u => u.TenantId).Distinct().ToArray();
        var tenantNames = await tenantRepository.GetNamesByIdsUnfilteredAsync(tenantIds, cancellationToken);

        if (query.PageOffset > 0 && query.PageOffset >= totalPages)
        {
            return Result<BackOfficeUsersResponse>.BadRequest($"The page offset '{query.PageOffset}' is greater than the total number of pages.");
        }

        var summaries = users.Select(u => new BackOfficeUserSummary(
                u.Id,
                u.TenantId,
                tenantNames.GetValueOrDefault(u.TenantId, string.Empty),
                u.Email,
                u.FirstName,
                u.LastName,
                u.Title,
                u.Role,
                u.EmailConfirmed,
                u.CreatedAt,
                u.LastSeenAt,
                u.Avatar.Url
            )
        ).ToArray();

        return new BackOfficeUsersResponse(totalCount, query.PageSize, totalPages, query.PageOffset, summaries);
    }
}
