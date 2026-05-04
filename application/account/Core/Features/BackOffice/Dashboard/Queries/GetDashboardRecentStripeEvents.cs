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
    TenantId TenantId,
    string TenantName,
    string? TenantLogoUrl,
    StripeEventType Type,
    SubscriptionPlan Plan,
    decimal? Amount,
    string? Currency,
    DateTimeOffset OccurredAt
);

[PublicAPI]
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum StripeEventType
{
    Subscribed,
    Upgraded,
    Downgraded,
    Canceled,
    PaymentFailed
}

public sealed class GetDashboardRecentStripeEventsQueryValidator : AbstractValidator<GetDashboardRecentStripeEventsQuery>
{
    public GetDashboardRecentStripeEventsQueryValidator()
    {
        RuleFor(x => x.Limit).InclusiveBetween(1, 50).WithMessage("Limit must be between 1 and 50.");
    }
}

public sealed class GetDashboardRecentStripeEventsHandler(
    ITenantRepository tenantRepository,
    ISubscriptionRepository subscriptionRepository
) : IRequestHandler<GetDashboardRecentStripeEventsQuery, Result<BackOfficeDashboardRecentStripeEventsResponse>>
{
    public async Task<Result<BackOfficeDashboardRecentStripeEventsResponse>> Handle(GetDashboardRecentStripeEventsQuery query, CancellationToken cancellationToken)
    {
        var tenants = await tenantRepository.GetAllUnfilteredAsync(cancellationToken);
        var tenantsById = tenants.ToDictionary(t => t.Id);
        var subscriptions = tenants.Length == 0
            ? []
            : await subscriptionRepository.GetByTenantIdsUnfilteredAsync(tenants.Select(t => t.Id).ToArray(), cancellationToken);

        var events = new List<BackOfficeDashboardStripeEvent>();
        foreach (var subscription in subscriptions)
        {
            if (!tenantsById.TryGetValue(subscription.TenantId, out var tenant)) continue;
            events.AddRange(BuildEventsForSubscription(tenant, subscription));
        }

        var ordered = events
            .OrderByDescending(e => e.OccurredAt)
            .Take(query.Limit)
            .ToArray();

        return new BackOfficeDashboardRecentStripeEventsResponse(ordered);
    }

    // Stripe lifecycle events are derived from existing aggregate fields rather than a webhook event log:
    // - Subscribed/Upgraded/Downgraded: walk PaymentTransactions ordered by date and compare amounts
    // - PaymentFailed: from FirstPaymentFailedAt
    // - Canceled: tenant is on Basis plan but has at least one successful past payment (CancellationReason captured)
    private static IEnumerable<BackOfficeDashboardStripeEvent> BuildEventsForSubscription(Tenant tenant, Subscription subscription)
    {
        var orderedTransactions = subscription.PaymentTransactions
            .Where(t => t.Status == PaymentTransactionStatus.Succeeded)
            .OrderBy(t => t.Date)
            .ToArray();

        decimal? previousAmount = null;
        foreach (var transaction in orderedTransactions)
        {
            var type = previousAmount switch
            {
                null => StripeEventType.Subscribed,
                _ when transaction.Amount > previousAmount.Value => StripeEventType.Upgraded,
                _ when transaction.Amount < previousAmount.Value => StripeEventType.Downgraded,
                _ => (StripeEventType?)null // a renewal at the same price; skip
            };

            if (type.HasValue)
            {
                yield return new BackOfficeDashboardStripeEvent(
                    tenant.Id,
                    tenant.Name,
                    tenant.Logo.Url,
                    type.Value,
                    subscription.Plan,
                    transaction.Amount,
                    transaction.Currency,
                    transaction.Date
                );
            }

            previousAmount = transaction.Amount;
        }

        if (subscription.FirstPaymentFailedAt.HasValue)
        {
            yield return new BackOfficeDashboardStripeEvent(
                tenant.Id,
                tenant.Name,
                tenant.Logo.Url,
                StripeEventType.PaymentFailed,
                subscription.Plan,
                subscription.CurrentPriceAmount,
                subscription.CurrentPriceCurrency,
                subscription.FirstPaymentFailedAt.Value
            );
        }

        // The domain does not store a canceled-at timestamp. We surface a Canceled event when the subscription is on
        // the free Basis plan AND has prior successful payments — the most recent of those payments is treated as the
        // approximate cancellation marker. Not perfect, but sufficient for an at-a-glance "recent activity" feed.
        if (subscription.Plan == SubscriptionPlan.Basis && orderedTransactions.Length > 0)
        {
            var lastTransaction = orderedTransactions[^1];
            yield return new BackOfficeDashboardStripeEvent(
                tenant.Id,
                tenant.Name,
                tenant.Logo.Url,
                StripeEventType.Canceled,
                subscription.Plan,
                lastTransaction.Amount,
                lastTransaction.Currency,
                lastTransaction.Date
            );
        }
    }
}
