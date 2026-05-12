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
///     trend period. Each successful charge adds <see cref="PaymentTransaction.AmountExcludingTax" /> to its
///     <see cref="PaymentTransaction.Date" /> bucket; a later credit note subtracts the same amount from its
///     <see cref="PaymentTransaction.CreditNotedAt" /> bucket, falling back to
///     <see cref="PaymentTransaction.RefundedAt" /> when the credit-note timestamp is missing; a
///     refund-without-credit-note subtracts from its <see cref="PaymentTransaction.RefundedAt" /> bucket. The chart shows
///     the running balance over
///     time — historic days reflect what was true at that point, and reversals dip the line on the day they
///     happened. Net contribution of any reversed transaction is zero, so the end-of-window cumulative
///     matches the Total Revenue tile (which sums only un-reversed transactions). The prior-period series is
///     the same all-time cumulative sampled across the equivalent window immediately before — when the
///     current line stays above the prior line, the platform is accumulating revenue faster than it did in
///     the prior window. Soft-delete semantic: historical revenue from soft-deleted tenants stays in the
///     curve because payment transactions are immutable historical money facts that outlive the tenant
///     lifecycle.
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
            // Add on the payment day for every charge that succeeded — even ones that were later reversed.
            // The chart is a running balance; reversals show up as a separate dip on the day they happened.
            if (transaction.Status is PaymentTransactionStatus.Succeeded or PaymentTransactionStatus.Refunded)
            {
                var paidOn = DateOnly.FromDateTime(transaction.Date.UtcDateTime);
                deltasByDay[paidOn] = deltasByDay.GetValueOrDefault(paidOn) + transaction.AmountExcludingTax;
            }

            // Subtract on the reversal day. A credit note encompasses any associated refund, so we subtract
            // once when both are present. Prefer the credit-note timestamp, then the refund timestamp, then
            // the payment date as a last-resort fallback for legacy rows where neither reversal timestamp
            // was captured; the same-day add+subtract still nets to zero so the end total matches the Total
            // Revenue tile.
            if (transaction.CreditNoteUrl is not null)
            {
                var creditNotedOn = DateOnly.FromDateTime((transaction.CreditNotedAt ?? transaction.RefundedAt ?? transaction.Date).UtcDateTime);
                deltasByDay[creditNotedOn] = deltasByDay.GetValueOrDefault(creditNotedOn) - transaction.AmountExcludingTax;
            }
            else if (transaction.RefundedAt is { } refundedAt)
            {
                var refundedOn = DateOnly.FromDateTime(refundedAt.UtcDateTime);
                deltasByDay[refundedOn] = deltasByDay.GetValueOrDefault(refundedOn) - transaction.AmountExcludingTax;
            }
        }

        return deltasByDay;
    }
}
