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
///     trend period. A successful payment adds <see cref="PaymentTransaction.AmountExcludingTax" /> to its
///     <see cref="PaymentTransaction.Date" /> bucket; a refunded payment additionally subtracts the same amount
///     from its <see cref="PaymentTransaction.RefundedAt" /> bucket. Each point in the curve is the running
///     total of those daily net deltas, starting at zero on day one of the period. The prior-period series is
///     computed the same way against the equivalent window immediately before the current period and also
///     starts at zero — so when the current line stays above the prior line, the platform is accumulating
///     revenue faster than the equivalent prior window. Soft-delete semantic: historical revenue from
///     soft-deleted tenants stays in the curve because payment transactions are immutable historical money
///     facts that outlive the tenant lifecycle.
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

        var points = new BackOfficeDashboardRevenueTrendPoint[days];
        var priorPoints = new BackOfficeDashboardRevenueTrendPoint[days];
        var currentCumulative = 0m;
        var priorCumulative = 0m;
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
            if (transaction.Status is PaymentTransactionStatus.Succeeded or PaymentTransactionStatus.Refunded)
            {
                var paidOn = DateOnly.FromDateTime(transaction.Date.UtcDateTime);
                deltasByDay[paidOn] = deltasByDay.GetValueOrDefault(paidOn) + transaction.AmountExcludingTax;
            }

            if (transaction.RefundedAt is { } refundedAt)
            {
                var refundedOn = DateOnly.FromDateTime(refundedAt.UtcDateTime);
                deltasByDay[refundedOn] = deltasByDay.GetValueOrDefault(refundedOn) - transaction.AmountExcludingTax;
            }
        }

        return deltasByDay;
    }
}
