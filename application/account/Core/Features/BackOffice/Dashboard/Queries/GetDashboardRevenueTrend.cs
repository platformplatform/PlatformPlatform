using Account.Features.Subscriptions.Domain;
using Account.Integrations.Stripe;
using FluentValidation;
using JetBrains.Annotations;
using SharedKernel.Cqrs;

namespace Account.Features.BackOffice.Dashboard.Queries;

[PublicAPI]
public sealed record GetDashboardRevenueTrendQuery(DashboardTrendPeriod Period = DashboardTrendPeriod.Last30Days)
    : IRequest<Result<BackOfficeDashboardRevenueTrendResponse>>;

[PublicAPI]
public sealed record BackOfficeDashboardRevenueTrendResponse(
    DashboardTrendPeriod Period,
    string? Currency,
    BackOfficeDashboardRevenueTrendPoint[] Points,
    BackOfficeDashboardRevenueTrendPoint[] PriorPoints
);

[PublicAPI]
public sealed record BackOfficeDashboardRevenueTrendPoint(DateOnly Date, decimal Revenue);

public sealed class GetDashboardRevenueTrendQueryValidator : AbstractValidator<GetDashboardRevenueTrendQuery>
{
    public GetDashboardRevenueTrendQueryValidator()
    {
        RuleFor(x => x.Period).Must(p => Enum.IsDefined(typeof(DashboardTrendPeriod), p)).WithMessage("Period must be one of Last7Days, Last30Days, or Last90Days.");
    }
}

/// <summary>
///     Cumulative ex-VAT revenue across every subscription's payment transactions, within the selected
///     trend period. A successful payment that was NOT later credit-noted or refunded adds
///     <see cref="PaymentTransaction.AmountExcludingTax" /> to its <see cref="PaymentTransaction.Date" /> bucket.
///     Credit-noted or refunded payments are excluded entirely — money returned to the customer is not revenue,
///     so the curve only reflects net-kept money. Each point in the curve is the running total of all daily
///     deltas from the earliest payment through that day — the same all-time cumulative the dashboard Total
///     Revenue tile reports, sampled across the period's days. So day one of the current window already reflects
///     every historical payment up to that day, not zero. The prior-period series is the same all-time
///     cumulative sampled across the equivalent window immediately before — when the current line stays above
///     the prior line, the platform is accumulating revenue faster than it did in the prior window. Soft-delete
///     semantic: historical revenue from soft-deleted tenants stays in the curve because payment transactions
///     are immutable historical money facts that outlive the tenant lifecycle.
/// </summary>
public sealed class GetDashboardRevenueTrendHandler(ISubscriptionRepository subscriptionRepository, IPlatformCurrencyProvider platformCurrencyProvider, TimeProvider timeProvider)
    : IRequestHandler<GetDashboardRevenueTrendQuery, Result<BackOfficeDashboardRevenueTrendResponse>>
{
    public async Task<Result<BackOfficeDashboardRevenueTrendResponse>> Handle(GetDashboardRevenueTrendQuery query, CancellationToken cancellationToken)
    {
        var days = DashboardTrendPeriods.GetDays(query.Period);
        var today = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
        var startDate = today.AddDays(-(days - 1));
        var priorStartDate = startDate.AddDays(-days);

        var subscriptions = await subscriptionRepository.GetAllWithTransactionsUnfilteredAsync(cancellationToken);
        var deltasByDay = ComputeDailyDeltas(subscriptions.SelectMany(s => s.PaymentTransactions));
        var cumulativeBeforePrior = deltasByDay.Where(d => d.Key < priorStartDate).Sum(d => d.Value);
        var cumulativeBeforeCurrent = deltasByDay.Where(d => d.Key < startDate).Sum(d => d.Value);

        var points = new BackOfficeDashboardRevenueTrendPoint[days];
        var priorPoints = new BackOfficeDashboardRevenueTrendPoint[days];
        var currentCumulative = cumulativeBeforeCurrent;
        var priorCumulative = cumulativeBeforePrior;
        for (var index = 0; index < days; index++)
        {
            var currentDate = startDate.AddDays(index);
            var priorDate = priorStartDate.AddDays(index);
            currentCumulative += deltasByDay.GetValueOrDefault(currentDate, 0m);
            priorCumulative += deltasByDay.GetValueOrDefault(priorDate, 0m);
            points[index] = new BackOfficeDashboardRevenueTrendPoint(currentDate, currentCumulative);
            priorPoints[index] = new BackOfficeDashboardRevenueTrendPoint(priorDate, priorCumulative);
        }

        return new BackOfficeDashboardRevenueTrendResponse(query.Period, platformCurrencyProvider.Currency, points, priorPoints);
    }

    private static Dictionary<DateOnly, decimal> ComputeDailyDeltas(IEnumerable<PaymentTransaction> transactions)
    {
        var deltasByDay = new Dictionary<DateOnly, decimal>();
        foreach (var transaction in transactions)
        {
            // Net revenue rule: only count successful payments that were NOT later reversed via credit note
            // or refund. A reversed transaction contributes 0 to revenue (no add on payment day, no subtract
            // on reversal day) — the line still climbs smoothly because we skip the row entirely. Matches the
            // Total Revenue KPI tile sum so the dashboard tile and the chart's last point always agree.
            if (transaction.Status is not PaymentTransactionStatus.Succeeded) continue;
            if (transaction.CreditNoteUrl is not null) continue;
            if (transaction.RefundedAt is not null) continue;

            var paidOn = DateOnly.FromDateTime(transaction.Date.UtcDateTime);
            deltasByDay[paidOn] = deltasByDay.GetValueOrDefault(paidOn) + transaction.AmountExcludingTax;
        }

        return deltasByDay;
    }
}
