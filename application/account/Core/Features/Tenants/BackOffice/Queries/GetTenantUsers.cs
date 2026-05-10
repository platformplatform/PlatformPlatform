using Account.Features.Tenants.Domain;
using Account.Features.Users.Domain;
using FluentValidation;
using JetBrains.Annotations;
using SharedKernel.Cqrs;
using SharedKernel.Domain;

namespace Account.Features.Tenants.BackOffice.Queries;

[PublicAPI]
public sealed record GetTenantUsersQuery(
    string? Search = null,
    UserRole[]? Roles = null,
    int PageOffset = 0,
    int PageSize = 25
) : IRequest<Result<TenantUsersResponse>>
{
    [JsonIgnore] // Removes from API contract
    public TenantId Id { get; init; } = null!;

    public string? Search { get; } = Search?.Trim().ToLower();

    public UserRole[] Roles { get; } = Roles ?? [];
}

[PublicAPI]
public sealed record TenantUsersResponse(int TotalCount, int PageSize, int TotalPages, int CurrentPageOffset, TenantUserSummary[] Users);

[PublicAPI]
public sealed record TenantUserSummary(
    UserId Id,
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

public sealed class GetTenantUsersQueryValidator : AbstractValidator<GetTenantUsersQuery>
{
    public GetTenantUsersQueryValidator()
    {
        RuleFor(x => x.Search).MaximumLength(100).WithMessage("Search must be no longer than 100 characters.");
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100).WithMessage("Page size must be between 1 and 100.");
        RuleFor(x => x.PageOffset).GreaterThanOrEqualTo(0).WithMessage("Page offset must be greater than or equal to 0.");
    }
}

public sealed class GetTenantUsersHandler(ITenantRepository tenantRepository, IUserRepository userRepository)
    : IRequestHandler<GetTenantUsersQuery, Result<TenantUsersResponse>>
{
    public async Task<Result<TenantUsersResponse>> Handle(GetTenantUsersQuery query, CancellationToken cancellationToken)
    {
        var tenant = await tenantRepository.GetByIdUnfilteredAsync(query.Id, cancellationToken);
        if (tenant is null)
        {
            return Result<TenantUsersResponse>.NotFound($"Tenant with id '{query.Id}' was not found.");
        }

        var (users, totalCount, totalPages) = await userRepository.SearchTenantUsersUnfilteredAsync(
            tenant.Id,
            query.Search,
            query.Roles,
            query.PageOffset,
            query.PageSize,
            cancellationToken
        );

        if (query.PageOffset > 0 && query.PageOffset >= totalPages)
        {
            return Result<TenantUsersResponse>.BadRequest($"The page offset '{query.PageOffset}' is greater than the total number of pages.");
        }

        var summaries = users.Select(u => new TenantUserSummary(
                u.Id,
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

        return new TenantUsersResponse(totalCount, query.PageSize, totalPages, query.PageOffset, summaries);
    }
}
