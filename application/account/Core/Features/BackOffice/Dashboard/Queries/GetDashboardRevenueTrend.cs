using Account.Features.Subscriptions.Domain;
using Account.Integrations.Stripe;
using JetBrains.Annotations;
using SharedKernel.Cqrs;

namespace Account.Features.BackOffice.Dashboard.Queries;

[PublicAPI]
public sealed record GetDashboardRevenueTrendQuery : IRequest<Result<BackOfficeDashboardRevenueTrendResponse>>;

[PublicAPI]
public sealed record BackOfficeDashboardRevenueTrendResponse(
    string? Currency,
    BackOfficeDashboardRevenueTrendPoint[] Points
);

[PublicAPI]
public sealed record BackOfficeDashboardRevenueTrendPoint(DateOnly Date, decimal Revenue);

/// <summary>
///     Daily ex-VAT revenue across every subscription's payment transactions, from the earliest event through
///     today. A successful payment adds <see cref="PaymentTransaction.AmountExcludingTax" /> to its
///     <see cref="PaymentTransaction.Date" /> bucket; a refunded payment additionally subtracts the same amount
///     from its <see cref="PaymentTransaction.RefundedAt" /> bucket, so the curve dips on the day the refund
///     happens, not the day of the original invoice. Empty days between the earliest and current day appear with
///     revenue zero so the line has no visual gaps. Soft-delete semantic: historical revenue from soft-deleted
///     tenants stays in the curve — payment transactions are immutable historical money facts that outlive the
///     tenant lifecycle.
/// </summary>
public sealed class GetDashboardRevenueTrendHandler(ISubscriptionRepository subscriptionRepository, IPlatformCurrencyProvider platformCurrencyProvider, TimeProvider timeProvider)
    : IRequestHandler<GetDashboardRevenueTrendQuery, Result<BackOfficeDashboardRevenueTrendResponse>>
{
    public async Task<Result<BackOfficeDashboardRevenueTrendResponse>> Handle(GetDashboardRevenueTrendQuery query, CancellationToken cancellationToken)
    {
        var subscriptions = await subscriptionRepository.GetAllWithTransactionsUnfilteredAsync(cancellationToken);
        var transactions = subscriptions.SelectMany(s => s.PaymentTransactions).ToArray();

        var totalsByDay = new Dictionary<DateOnly, decimal>();

        foreach (var transaction in transactions)
        {
            if (transaction.Status is PaymentTransactionStatus.Succeeded or PaymentTransactionStatus.Refunded)
            {
                var paidOn = DateOnly.FromDateTime(transaction.Date.UtcDateTime);
                totalsByDay[paidOn] = totalsByDay.GetValueOrDefault(paidOn) + transaction.AmountExcludingTax;
            }

            if (transaction.RefundedAt is { } refundedAt)
            {
                var refundedOn = DateOnly.FromDateTime(refundedAt.UtcDateTime);
                totalsByDay[refundedOn] = totalsByDay.GetValueOrDefault(refundedOn) - transaction.AmountExcludingTax;
            }
        }

        if (totalsByDay.Count == 0)
        {
            return new BackOfficeDashboardRevenueTrendResponse(platformCurrencyProvider.Currency, []);
        }

        var earliestDay = totalsByDay.Keys.Min();
        var currentDay = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);

        var points = new List<BackOfficeDashboardRevenueTrendPoint>();
        for (var day = earliestDay; day <= currentDay; day = day.AddDays(1))
        {
            points.Add(new BackOfficeDashboardRevenueTrendPoint(day, totalsByDay.GetValueOrDefault(day, 0m)));
        }

        return new BackOfficeDashboardRevenueTrendResponse(platformCurrencyProvider.Currency, [.. points]);
    }
}
