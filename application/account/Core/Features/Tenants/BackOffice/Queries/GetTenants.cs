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
    string? LogoUrl,
    SubscriptionPlan Plan,
    decimal? MonthlyRecurringRevenue,
    decimal? ScheduledPriceAmount,
    string? Currency,
    DateTimeOffset? RenewalDate,
    PlannedSubscriptionChange? PlannedChange,
    bool HasEverSubscribed,
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
    Plan,
    MonthlyRecurringRevenue,
    RenewalDate,
    Status,
    Country,
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
            (SortableTenantProperties.Plan, SortOrder.Ascending) => summaries.OrderBy(s => s.Plan).ThenBy(s => s.Name),
            (SortableTenantProperties.Plan, _) => summaries.OrderByDescending(s => s.Plan).ThenBy(s => s.Name),
            (SortableTenantProperties.MonthlyRecurringRevenue, SortOrder.Ascending) => summaries.OrderBy(s => s.MonthlyRecurringRevenue ?? 0).ThenBy(s => s.Name),
            (SortableTenantProperties.MonthlyRecurringRevenue, _) => summaries.OrderByDescending(s => s.MonthlyRecurringRevenue ?? 0).ThenBy(s => s.Name),
            (SortableTenantProperties.RenewalDate, SortOrder.Ascending) => summaries.OrderBy(s => s.RenewalDate ?? DateTimeOffset.MaxValue).ThenBy(s => s.Name),
            (SortableTenantProperties.RenewalDate, _) => summaries.OrderByDescending(s => s.RenewalDate ?? DateTimeOffset.MinValue).ThenBy(s => s.Name),
            (SortableTenantProperties.Status, SortOrder.Ascending) => summaries.OrderBy(StatusSortKey).ThenBy(s => s.Name),
            (SortableTenantProperties.Status, _) => summaries.OrderByDescending(StatusSortKey).ThenBy(s => s.Name),
            (SortableTenantProperties.Country, SortOrder.Ascending) => summaries.OrderBy(s => s.Country ?? string.Empty).ThenBy(s => s.Name),
            (SortableTenantProperties.Country, _) => summaries.OrderByDescending(s => s.Country ?? string.Empty).ThenBy(s => s.Name),
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

    private static int StatusSortKey(TenantSummary summary)
    {
        // Order: Active paid, Downgrading, Canceling, Basis. Stable secondary sort on Name handled by caller.
        return summary switch
        {
            { PlannedChange: PlannedSubscriptionChange.Cancellation } => 2,
            { PlannedChange: PlannedSubscriptionChange.ScheduledPlanChange } => 1,
            { Plan: not SubscriptionPlan.Basis } => 0,
            _ => 3
        };
    }

    private static TenantSummary MapTenantSummary(Tenant tenant, Subscription? subscription)
    {
        var plannedChange = subscription switch
        {
            { CancelAtPeriodEnd: true } => PlannedSubscriptionChange.Cancellation,
            { ScheduledPlan: not null } => PlannedSubscriptionChange.ScheduledPlanChange,
            _ => (PlannedSubscriptionChange?)null
        };

        var hasEverSubscribed = subscription?.PaymentTransactions
            .Any(transaction => transaction.Status == PaymentTransactionStatus.Succeeded) == true;

        return new TenantSummary(
            tenant.Id,
            tenant.Name,
            tenant.Logo.Url,
            tenant.Plan,
            subscription?.CurrentPriceAmount,
            subscription?.ScheduledPriceAmount,
            subscription?.CurrentPriceCurrency,
            subscription?.CurrentPeriodEnd,
            plannedChange,
            hasEverSubscribed,
            subscription?.BillingInfo?.Address?.Country,
            tenant.CreatedAt
        );
    }
}
