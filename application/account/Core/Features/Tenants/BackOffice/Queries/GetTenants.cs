using Account.Features.Subscriptions.Domain;
using Account.Features.Tenants.Domain;
using Account.Features.Users.Domain;
using FluentValidation;
using JetBrains.Annotations;
using SharedKernel.Cqrs;
using SharedKernel.Domain;
using SharedKernel.Persistence;

namespace Account.Features.Tenants.BackOffice.Queries;

[PublicAPI]
public sealed record GetTenantsQuery(
    string? Search = null,
    SubscriptionPlan[]? Plans = null,
    TenantStatusFilter[]? Statuses = null,
    bool Unsynced = false,
    bool DriftDetected = false,
    SortableTenantProperties OrderBy = SortableTenantProperties.ModifiedAt,
    SortOrder SortOrder = SortOrder.Descending,
    int PageOffset = 0,
    int PageSize = 25
) : IRequest<Result<TenantsResponse>>
{
    public string? Search { get; } = Search?.Trim().ToLower();

    public SubscriptionPlan[] Plans { get; } = Plans ?? [];

    public TenantStatusFilter[] Statuses { get; } = Statuses ?? [];
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
    DateTimeOffset CreatedAt,
    DateTimeOffset? ModifiedAt,
    TenantOwnerSummary? Owner
)
{
    // Shared factory used by every back-office view that renders the rich tenant row (accounts list + feature-flag
    // override views). Keeping construction co-located with the record avoids drift between call sites.
    public static TenantSummary FromAggregate(Tenant tenant, Subscription? subscription, User? owner)
    {
        var plannedChange = subscription switch
        {
            { CancelAtPeriodEnd: true } => PlannedSubscriptionChange.Cancellation,
            { ScheduledPlan: not null } => PlannedSubscriptionChange.ScheduledPlanChange,
            _ => (PlannedSubscriptionChange?)null
        };

        // Refunded counts as "ever subscribed" — money flowed in before being credited back, so the tenant did pay at
        // some point. Distinguishes a refunded customer (Canceled) from never having paid at all (Free).
        var hasEverSubscribed = subscription?.PaymentTransactions
            .Any(transaction => transaction.Status is PaymentTransactionStatus.Succeeded or PaymentTransactionStatus.Refunded) == true;

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
            tenant.CreatedAt,
            tenant.ModifiedAt,
            owner is null ? null : new TenantOwnerSummary(owner.Id, owner.FirstName, owner.LastName, owner.Email)
        );
    }
}

[PublicAPI]
public sealed record TenantOwnerSummary(UserId UserId, string? FirstName, string? LastName, string Email);

[PublicAPI]
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum PlannedSubscriptionChange
{
    Cancellation,
    ScheduledPlanChange
}

[PublicAPI]
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum TenantStatusFilter
{
    Active,
    Downgrading,
    Canceling,
    Canceled,
    Free
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
    CreatedAt,
    ModifiedAt
}

public sealed class GetTenantsQueryValidator : AbstractValidator<GetTenantsQuery>
{
    public GetTenantsQueryValidator()
    {
        RuleFor(x => x.Search).MaximumLength(100).WithMessage("Search must be no longer than 100 characters.");
        RuleFor(x => x.Plans.Length).LessThanOrEqualTo(10).WithMessage("Plans filter must contain no more than 10 values.");
        RuleFor(x => x.Statuses.Length).LessThanOrEqualTo(10).WithMessage("Statuses filter must contain no more than 10 values.");
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100).WithMessage("Page size must be between 1 and 100.");
        RuleFor(x => x.PageOffset).GreaterThanOrEqualTo(0).WithMessage("Page offset must be greater than or equal to 0.");
    }
}

public sealed class GetTenantsHandler(ITenantRepository tenantRepository, ISubscriptionRepository subscriptionRepository, IBillingEventRepository billingEventRepository, IUserRepository userRepository)
    : IRequestHandler<GetTenantsQuery, Result<TenantsResponse>>
{
    public async Task<Result<TenantsResponse>> Handle(GetTenantsQuery query, CancellationToken cancellationToken)
    {
        var tenants = await tenantRepository.SearchAllTenantsAsync(query.Search, query.Plans, cancellationToken);

        var tenantIds = tenants.Select(t => t.Id).ToArray();
        var subscriptions = tenantIds.Length == 0
            ? []
            : await subscriptionRepository.GetByTenantIdsUnfilteredAsync(tenantIds, cancellationToken);
        var subscriptionsByTenantId = subscriptions.ToDictionary(s => s.TenantId);

        // Tenant-issue filters from the back-office banners. DriftDetected is a per-subscription flag set
        // by the writer when the replayer hits an Unclassified event; Unsynced means a paid subscription
        // has no BillingEvent rows yet (the dashboard MRR trend silently under-counts these).
        if (query.DriftDetected)
        {
            tenants = tenants.Where(t => subscriptionsByTenantId.GetValueOrDefault(t.Id)?.HasDriftDetected == true).ToArray();
        }

        if (query.Unsynced)
        {
            var subscriptionIdsWithEvents = subscriptions.Length == 0
                ? new HashSet<SubscriptionId>()
                : await billingEventRepository.GetSubscriptionIdsWithEventsUnfilteredAsync([.. subscriptions.Select(s => s.Id)], cancellationToken);
            tenants = tenants.Where(t =>
                {
                    var subscription = subscriptionsByTenantId.GetValueOrDefault(t.Id);
                    return subscription is { CurrentPriceAmount: not null } && !subscriptionIdsWithEvents.Contains(subscription.Id);
                }
            ).ToArray();
        }

        var ownerByTenantId = tenants.Length == 0
            ? new Dictionary<TenantId, User>()
            : await userRepository.GetFirstOwnerByTenantIdsUnfilteredAsync(tenants.Select(t => t.Id).ToArray(), cancellationToken);

        var summaries = tenants.Select(tenant => TenantSummary.FromAggregate(
                tenant,
                subscriptionsByTenantId.GetValueOrDefault(tenant.Id),
                ownerByTenantId.GetValueOrDefault(tenant.Id)
            )
        ).ToArray();

        if (query.Statuses.Length > 0)
        {
            summaries = summaries.Where(s => query.Statuses.Contains(GetStatus(s))).ToArray();
        }

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
            // ModifiedAt is null until the tenant is touched; treat that as "latest activity = creation"
            // so the default view shows recently-changed and brand-new tenants together at the top.
            (SortableTenantProperties.ModifiedAt, SortOrder.Ascending) => summaries.OrderBy(s => s.ModifiedAt ?? s.CreatedAt).ThenBy(s => s.Name),
            (SortableTenantProperties.ModifiedAt, _) => summaries.OrderByDescending(s => s.ModifiedAt ?? s.CreatedAt).ThenBy(s => s.Name),
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
        return GetStatus(summary) switch
        {
            TenantStatusFilter.Active => 0,
            TenantStatusFilter.Downgrading => 1,
            TenantStatusFilter.Canceling => 2,
            TenantStatusFilter.Canceled => 3,
            TenantStatusFilter.Free => 4,
            _ => 5
        };
    }

    private static TenantStatusFilter GetStatus(TenantSummary summary)
    {
        return summary switch
        {
            { PlannedChange: PlannedSubscriptionChange.Cancellation } => TenantStatusFilter.Canceling,
            { PlannedChange: PlannedSubscriptionChange.ScheduledPlanChange } => TenantStatusFilter.Downgrading,
            { Plan: not SubscriptionPlan.Basis } => TenantStatusFilter.Active,
            { HasEverSubscribed: true } => TenantStatusFilter.Canceled,
            _ => TenantStatusFilter.Free
        };
    }
}
