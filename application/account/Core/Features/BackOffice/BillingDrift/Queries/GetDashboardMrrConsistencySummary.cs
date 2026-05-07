using Account.Features.Subscriptions.Domain;
using Account.Features.Subscriptions.Shared;
using JetBrains.Annotations;
using SharedKernel.Cqrs;

namespace Account.Features.BackOffice.BillingDrift.Queries;

[PublicAPI]
public sealed record GetDashboardMrrConsistencySummaryQuery : IRequest<Result<DashboardMrrConsistencySummaryResponse>>;

[PublicAPI]
public sealed record DashboardMrrConsistencySummaryResponse(decimal KpiMonthlyRecurringRevenue, decimal TrendLatestMonthlyRecurringRevenue, string Currency);

public sealed class GetDashboardMrrConsistencySummaryHandler(ISubscriptionRepository subscriptionRepository, IBillingEventRepository billingEventRepository)
    : IRequestHandler<GetDashboardMrrConsistencySummaryQuery, Result<DashboardMrrConsistencySummaryResponse>>
{
    private const string DefaultCurrency = "DKK";

    public async Task<Result<DashboardMrrConsistencySummaryResponse>> Handle(GetDashboardMrrConsistencySummaryQuery query, CancellationToken cancellationToken)
    {
        var paidSubscriptions = await subscriptionRepository.GetAllActiveUnfilteredAsync(cancellationToken);
        var kpiMrr = paidSubscriptions.Sum(MrrCalculator.ForwardMrr);

        // Trend-latest MRR — mirrors GetDashboardMrrTrendHandler: per subscription, take the latest event's NewAmount.
        var events = await billingEventRepository.GetMrrChangeEventsUnfilteredAsync(cancellationToken);
        var trendLatestMrr = events
            .GroupBy(e => e.SubscriptionId)
            .Sum(g => g.OrderByDescending(e => e.OccurredAt).First().NewAmount ?? 0m);

        return new DashboardMrrConsistencySummaryResponse(kpiMrr, trendLatestMrr, DefaultCurrency);
    }
}
