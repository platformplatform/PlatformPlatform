using Account.Features.Subscriptions.Domain;
using Account.Features.Subscriptions.Shared;
using Account.Integrations.Stripe;
using JetBrains.Annotations;
using SharedKernel.Cqrs;

namespace Account.Features.BackOffice.BillingDrift.Queries;

[PublicAPI]
public sealed record GetDashboardMrrConsistencySummaryQuery : IRequest<Result<DashboardMrrConsistencySummaryResponse>>;

[PublicAPI]
public sealed record DashboardMrrConsistencySummaryResponse(decimal KpiMonthlyRecurringRevenue, decimal TrendLatestMonthlyRecurringRevenue, string? Currency);

public sealed class GetDashboardMrrConsistencySummaryHandler(ISubscriptionRepository subscriptionRepository, IBillingEventRepository billingEventRepository, IPlatformCurrencyProvider platformCurrencyProvider)
    : IRequestHandler<GetDashboardMrrConsistencySummaryQuery, Result<DashboardMrrConsistencySummaryResponse>>
{
    // Sub-cent diffs between KPI and trend-latest MRR are accounting noise, not drift. The FE banner
    // does strict equality so we snap trend-latest to KPI when the absolute diff is below tolerance.
    private const decimal ToleranceAmount = 0.01m;

    public async Task<Result<DashboardMrrConsistencySummaryResponse>> Handle(GetDashboardMrrConsistencySummaryQuery query, CancellationToken cancellationToken)
    {
        var paidSubscriptions = await subscriptionRepository.GetAllActiveUnfilteredAsync(cancellationToken);
        var kpiMrr = paidSubscriptions.Sum(MrrCalculator.ForwardMrr);

        // Trend-latest MRR — mirrors GetDashboardMrrTrendHandler: per subscription, take the latest event's NewAmount.
        var events = await billingEventRepository.GetMrrChangeEventsUnfilteredAsync(cancellationToken);
        var trendLatestMrr = events
            .GroupBy(e => e.SubscriptionId)
            .Sum(g => g.OrderByDescending(e => e.OccurredAt).First().NewAmount ?? 0m);

        if (Math.Abs(kpiMrr - trendLatestMrr) < ToleranceAmount)
        {
            trendLatestMrr = kpiMrr;
        }

        return new DashboardMrrConsistencySummaryResponse(kpiMrr, trendLatestMrr, platformCurrencyProvider.Currency);
    }
}
