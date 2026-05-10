using Account.Features.Subscriptions.Domain;
using Account.Features.Tenants.BackOffice.Queries;
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
    SortableBackOfficeUserProperties OrderBy = SortableBackOfficeUserProperties.LastSeenAt,
    SortOrder SortOrder = SortOrder.Descending,
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
    SubscriptionPlan TenantPlan,
    PlannedSubscriptionChange? TenantPlannedChange,
    bool TenantHasEverSubscribed,
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
        // Search is optional. When omitted or empty, the page lists every user newest-first. When provided, the cap of
        // 100 characters guards against malicious input — the WebApp normally sends short tokens.
        RuleFor(x => x.Search).MaximumLength(100).WithMessage("Search must be at most 100 characters.");
        RuleFor(x => x.Roles.Length).LessThanOrEqualTo(10).WithMessage("Roles filter must contain no more than 10 values.");
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100).WithMessage("Page size must be between 1 and 100.");
        RuleFor(x => x.PageOffset).GreaterThanOrEqualTo(0).WithMessage("Page offset must be greater than or equal to 0.");
    }
}

public sealed class GetBackOfficeUsersHandler(
    IUserRepository userRepository,
    ITenantRepository tenantRepository,
    ISubscriptionRepository subscriptionRepository,
    TimeProvider timeProvider
) : IRequestHandler<GetBackOfficeUsersQuery, Result<BackOfficeUsersResponse>>
{
    public async Task<Result<BackOfficeUsersResponse>> Handle(GetBackOfficeUsersQuery query, CancellationToken cancellationToken)
    {
        var (users, totalCount, totalPages) = await userRepository.SearchAllUsersUnfilteredAsync(
            query.Search ?? "",
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
        var tenants = await tenantRepository.GetByIdsUnfilteredAsync(tenantIds, cancellationToken);
        var tenantsById = tenants.ToDictionary(t => t.Id);
        var subscriptions = await subscriptionRepository.GetByTenantIdsUnfilteredAsync(tenantIds, cancellationToken);
        var subscriptionsByTenantId = subscriptions.ToDictionary(s => s.TenantId);

        if (query.PageOffset > 0 && query.PageOffset >= totalPages)
        {
            return Result<BackOfficeUsersResponse>.BadRequest($"The page offset '{query.PageOffset}' is greater than the total number of pages.");
        }

        var summaries = users.Select(u =>
            {
                var tenant = tenantsById.GetValueOrDefault(u.TenantId);
                var subscription = subscriptionsByTenantId.GetValueOrDefault(u.TenantId);
                var plannedChange = subscription switch
                {
                    { CancelAtPeriodEnd: true } => PlannedSubscriptionChange.Cancellation,
                    { ScheduledPlan: not null } => PlannedSubscriptionChange.ScheduledPlanChange,
                    _ => (PlannedSubscriptionChange?)null
                };
                var hasEverSubscribed = subscription?.PaymentTransactions
                    .Any(transaction => transaction.Status == PaymentTransactionStatus.Succeeded) == true;
                return new BackOfficeUserSummary(
                    u.Id,
                    u.TenantId,
                    tenant?.Name ?? string.Empty,
                    tenant?.Plan ?? SubscriptionPlan.Basis,
                    plannedChange,
                    hasEverSubscribed,
                    u.Email,
                    u.FirstName,
                    u.LastName,
                    u.Title,
                    u.Role,
                    u.EmailConfirmed,
                    u.CreatedAt,
                    u.LastSeenAt,
                    u.Avatar.Url
                );
            }
        ).ToArray();

        return new BackOfficeUsersResponse(totalCount, query.PageSize, totalPages, query.PageOffset, summaries);
    }
}
