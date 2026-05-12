using Account.Features.Subscriptions.Domain;
using Account.Features.Tenants.Domain;
using FluentValidation;
using JetBrains.Annotations;
using SharedKernel.Cqrs;
using SharedKernel.Domain;

namespace Account.Features.BackOffice.Dashboard.Queries;

[PublicAPI]
public sealed record GetDashboardRecentStripeEventsQuery(int Limit = 6)
    : IRequest<Result<BackOfficeDashboardRecentStripeEventsResponse>>;

[PublicAPI]
public sealed record BackOfficeDashboardRecentStripeEventsResponse(BackOfficeDashboardStripeEvent[] Events);

[PublicAPI]
public sealed record BackOfficeDashboardStripeEvent(
    BillingEventId Id,
    TenantId TenantId,
    string TenantName,
    string? TenantLogoUrl,
    BillingEventType Type,
    SubscriptionPlan? FromPlan,
    SubscriptionPlan? ToPlan,
    decimal? AmountDelta,
    string? Currency,
    DateTimeOffset OccurredAt
);

public sealed class GetDashboardRecentStripeEventsQueryValidator : AbstractValidator<GetDashboardRecentStripeEventsQuery>
{
    public GetDashboardRecentStripeEventsQueryValidator()
    {
        RuleFor(x => x.Limit).InclusiveBetween(1, 50).WithMessage("Limit must be between 1 and 50.");
    }
}

public sealed class GetDashboardRecentStripeEventsHandler(IBillingEventRepository billingEventRepository, ITenantRepository tenantRepository)
    : IRequestHandler<GetDashboardRecentStripeEventsQuery, Result<BackOfficeDashboardRecentStripeEventsResponse>>
{
    public async Task<Result<BackOfficeDashboardRecentStripeEventsResponse>> Handle(GetDashboardRecentStripeEventsQuery query, CancellationToken cancellationToken)
    {
        var billingEvents = await billingEventRepository.GetRecentUnfilteredAsync(query.Limit, cancellationToken);
        if (billingEvents.Length == 0) return new BackOfficeDashboardRecentStripeEventsResponse([]);

        var tenantIds = billingEvents.Select(e => e.TenantId).Distinct().ToArray();
        var tenants = await tenantRepository.GetByIdsUnfilteredAsync(tenantIds, cancellationToken);
        var tenantsById = tenants.ToDictionary(t => t.Id);

        var events = billingEvents
            .Where(e => tenantsById.ContainsKey(e.TenantId))
            .Select(e =>
                {
                    var tenant = tenantsById[e.TenantId];
                    return new BackOfficeDashboardStripeEvent(
                        e.Id,
                        tenant.Id,
                        tenant.Name,
                        tenant.Logo.Url,
                        e.EventType,
                        e.FromPlan,
                        e.ToPlan,
                        e.AmountDelta,
                        e.Currency,
                        e.OccurredAt
                    );
                }
            )
            .ToArray();

        return new BackOfficeDashboardRecentStripeEventsResponse(events);
    }
}
