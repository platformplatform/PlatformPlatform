using Account.Features.Subscriptions.Domain;
using Account.Features.Tenants.Domain;
using FluentValidation;
using JetBrains.Annotations;
using SharedKernel.Cqrs;
using SharedKernel.Domain;
using SharedKernel.Persistence;

namespace Account.Features.BackOffice.BillingEvents.Queries;

[PublicAPI]
public sealed record GetBackOfficeBillingEventsQuery(
    string? Search = null,
    BillingEventType[]? EventTypes = null,
    DateTimeOffset? OccurredFrom = null,
    DateTimeOffset? OccurredTo = null,
    TenantId? TenantId = null,
    SortableBillingEventProperties OrderBy = SortableBillingEventProperties.OccurredAt,
    SortOrder SortOrder = SortOrder.Descending,
    int PageOffset = 0,
    int PageSize = 25
) : IRequest<Result<BillingEventsResponse>>
{
    public string? Search { get; } = Search?.Trim().ToLower();

    public BillingEventType[] EventTypes { get; } = EventTypes ?? [];
}

[PublicAPI]
public sealed record BillingEventsResponse(int TotalCount, int PageSize, int TotalPages, int CurrentPageOffset, BillingEventSummary[] BillingEvents);

[PublicAPI]
public sealed record BillingEventSummary(
    BillingEventId Id,
    TenantId TenantId,
    string TenantName,
    string? TenantLogoUrl,
    string? Country,
    BillingEventType EventType,
    SubscriptionPlan? FromPlan,
    SubscriptionPlan? ToPlan,
    decimal? AmountDelta,
    decimal? PreviousAmount,
    decimal? NewAmount,
    decimal CommittedMrr,
    string? Currency,
    DateTimeOffset OccurredAt
);

[PublicAPI]
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum SortableBillingEventProperties
{
    OccurredAt,
    EventType,
    TenantName
}

public sealed class GetBackOfficeBillingEventsQueryValidator : AbstractValidator<GetBackOfficeBillingEventsQuery>
{
    public GetBackOfficeBillingEventsQueryValidator()
    {
        RuleFor(x => x.Search).MaximumLength(100).WithMessage("Search must be no longer than 100 characters.");
        RuleFor(x => x.EventTypes.Length).LessThanOrEqualTo(25).WithMessage("Event types filter must contain no more than 25 values.");
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100).WithMessage("Page size must be between 1 and 100.");
        RuleFor(x => x.PageOffset).GreaterThanOrEqualTo(0).WithMessage("Page offset must be greater than or equal to 0.");
    }
}

public sealed class GetBackOfficeBillingEventsHandler(
    IBillingEventRepository billingEventRepository,
    ITenantRepository tenantRepository,
    ISubscriptionRepository subscriptionRepository
) : IRequestHandler<GetBackOfficeBillingEventsQuery, Result<BillingEventsResponse>>
{
    public async Task<Result<BillingEventsResponse>> Handle(GetBackOfficeBillingEventsQuery query, CancellationToken cancellationToken)
    {
        var billingEvents = await billingEventRepository.SearchAllUnfilteredAsync(query.EventTypes, query.OccurredFrom, query.OccurredTo, cancellationToken);

        if (query.TenantId is not null)
        {
            billingEvents = billingEvents.Where(e => e.TenantId == query.TenantId).ToArray();
        }

        var tenantIds = billingEvents.Select(e => e.TenantId).Distinct().ToArray();
        var tenants = tenantIds.Length == 0
            ? []
            : await tenantRepository.GetByIdsUnfilteredAsync(tenantIds, cancellationToken);
        var tenantsById = tenants.ToDictionary(t => t.Id);

        var subscriptions = tenantIds.Length == 0
            ? []
            : await subscriptionRepository.GetByTenantIdsUnfilteredAsync(tenantIds, cancellationToken);
        var subscriptionsByTenantId = subscriptions.ToDictionary(s => s.TenantId);

        var summaries = billingEvents
            .Where(e => tenantsById.ContainsKey(e.TenantId))
            .Select(e =>
                {
                    var tenant = tenantsById[e.TenantId];
                    var subscription = subscriptionsByTenantId.GetValueOrDefault(tenant.Id);
                    return new BillingEventSummary(
                        e.Id,
                        tenant.Id,
                        tenant.Name,
                        tenant.Logo.Url,
                        subscription?.BillingInfo?.Address?.Country,
                        e.EventType,
                        e.FromPlan,
                        e.ToPlan,
                        e.AmountDelta,
                        e.PreviousAmount,
                        e.NewAmount,
                        e.CommittedMrr,
                        e.Currency,
                        e.OccurredAt
                    );
                }
            )
            .ToArray();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            summaries = summaries.Where(s => s.TenantName.ToLower().Contains(query.Search)).ToArray();
        }

        var ordered = (query.OrderBy, query.SortOrder) switch
        {
            (SortableBillingEventProperties.EventType, SortOrder.Ascending) => summaries.OrderBy(s => s.EventType).ThenByDescending(s => s.OccurredAt),
            (SortableBillingEventProperties.EventType, _) => summaries.OrderByDescending(s => s.EventType).ThenByDescending(s => s.OccurredAt),
            (SortableBillingEventProperties.TenantName, SortOrder.Ascending) => summaries.OrderBy(s => s.TenantName).ThenByDescending(s => s.OccurredAt),
            (SortableBillingEventProperties.TenantName, _) => summaries.OrderByDescending(s => s.TenantName).ThenByDescending(s => s.OccurredAt),
            (SortableBillingEventProperties.OccurredAt, SortOrder.Ascending) => summaries.OrderBy(s => s.OccurredAt),
            _ => summaries.OrderByDescending(s => s.OccurredAt)
        };

        var totalCount = summaries.Length;
        var totalPages = totalCount == 0 ? 0 : (totalCount - 1) / query.PageSize + 1;
        if (query.PageOffset > 0 && query.PageOffset >= totalPages)
        {
            return Result<BillingEventsResponse>.BadRequest($"The page offset '{query.PageOffset}' is greater than the total number of pages.");
        }

        var paged = ordered.Skip(query.PageOffset * query.PageSize).Take(query.PageSize).ToArray();

        return new BillingEventsResponse(totalCount, query.PageSize, totalPages, query.PageOffset, paged);
    }
}
