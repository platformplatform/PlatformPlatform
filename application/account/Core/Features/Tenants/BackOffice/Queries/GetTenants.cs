using Account.Features.Subscriptions.Domain;
using Account.Features.Tenants.Domain;
using FluentValidation;
using JetBrains.Annotations;
using SharedKernel.Cqrs;
using SharedKernel.Domain;
using SharedKernel.Persistence;

namespace Account.Features.Tenants.BackOffice.Queries;

[PublicAPI]
public sealed record GetTenantsQuery(
    string? Search = null,
    SubscriptionPlan? Plan = null,
    SortableTenantProperties OrderBy = SortableTenantProperties.Name,
    SortOrder SortOrder = SortOrder.Ascending,
    int PageOffset = 0,
    int PageSize = 25
) : IRequest<Result<TenantsResponse>>
{
    public string? Search { get; } = Search?.Trim().ToLower();
}

[PublicAPI]
public sealed record TenantsResponse(int TotalCount, int PageSize, int TotalPages, int CurrentPageOffset, TenantSummary[] Tenants);

[PublicAPI]
public sealed record TenantSummary(
    TenantId Id,
    string Name,
    SubscriptionPlan Plan,
    decimal? MonthlyRecurringRevenue,
    string? Currency,
    DateTimeOffset? RenewalDate,
    PlannedSubscriptionChange? PlannedChange,
    string? Country,
    DateTimeOffset CreatedAt
);

[PublicAPI]
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum PlannedSubscriptionChange
{
    Cancellation,
    ScheduledPlanChange
}

[PublicAPI]
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum SortableTenantProperties
{
    Name,
    MonthlyRecurringRevenue,
    CreatedAt
}

public sealed class GetTenantsQueryValidator : AbstractValidator<GetTenantsQuery>
{
    public GetTenantsQueryValidator()
    {
        RuleFor(x => x.Search).MaximumLength(100).WithMessage("Search must be no longer than 100 characters.");
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100).WithMessage("Page size must be between 1 and 100.");
        RuleFor(x => x.PageOffset).GreaterThanOrEqualTo(0).WithMessage("Page offset must be greater than or equal to 0.");
    }
}

public sealed class GetTenantsHandler(ITenantRepository tenantRepository, ISubscriptionRepository subscriptionRepository)
    : IRequestHandler<GetTenantsQuery, Result<TenantsResponse>>
{
    public async Task<Result<TenantsResponse>> Handle(GetTenantsQuery query, CancellationToken cancellationToken)
    {
        var tenants = await tenantRepository.SearchAllTenantsAsync(query.Search, query.Plan, cancellationToken);

        var tenantIds = tenants.Select(t => t.Id).ToArray();
        var subscriptions = tenantIds.Length == 0
            ? []
            : await subscriptionRepository.GetByTenantIdsUnfilteredAsync(tenantIds, cancellationToken);
        var subscriptionsByTenantId = subscriptions.ToDictionary(s => s.TenantId);

        var summaries = tenants.Select(tenant => MapTenantSummary(tenant, subscriptionsByTenantId.GetValueOrDefault(tenant.Id))).ToArray();

        var ordered = (query.OrderBy, query.SortOrder) switch
        {
            (SortableTenantProperties.MonthlyRecurringRevenue, SortOrder.Ascending) => summaries.OrderBy(s => s.MonthlyRecurringRevenue ?? 0).ThenBy(s => s.Name),
            (SortableTenantProperties.MonthlyRecurringRevenue, _) => summaries.OrderByDescending(s => s.MonthlyRecurringRevenue ?? 0).ThenBy(s => s.Name),
            (SortableTenantProperties.CreatedAt, SortOrder.Ascending) => summaries.OrderBy(s => s.CreatedAt),
            (SortableTenantProperties.CreatedAt, _) => summaries.OrderByDescending(s => s.CreatedAt),
            (SortableTenantProperties.Name, SortOrder.Descending) => summaries.OrderByDescending(s => s.Name),
            _ => summaries.OrderBy(s => s.Name)
        };

        var totalCount = summaries.Length;
        var totalPages = totalCount == 0 ? 0 : (totalCount - 1) / query.PageSize + 1;
        if (query.PageOffset > 0 && query.PageOffset >= totalPages)
        {
            return Result<TenantsResponse>.BadRequest($"The page offset '{query.PageOffset}' is greater than the total number of pages.");
        }

        var paged = ordered.Skip(query.PageOffset * query.PageSize).Take(query.PageSize).ToArray();

        return new TenantsResponse(totalCount, query.PageSize, totalPages, query.PageOffset, paged);
    }

    private static TenantSummary MapTenantSummary(Tenant tenant, Subscription? subscription)
    {
        var plannedChange = subscription switch
        {
            { CancelAtPeriodEnd: true } => PlannedSubscriptionChange.Cancellation,
            { ScheduledPlan: not null } => PlannedSubscriptionChange.ScheduledPlanChange,
            _ => (PlannedSubscriptionChange?)null
        };

        return new TenantSummary(
            tenant.Id,
            tenant.Name,
            tenant.Plan,
            subscription?.CurrentPriceAmount,
            subscription?.CurrentPriceCurrency,
            subscription?.CurrentPeriodEnd,
            plannedChange,
            subscription?.BillingInfo?.Address?.Country,
            tenant.CreatedAt
        );
    }
}
