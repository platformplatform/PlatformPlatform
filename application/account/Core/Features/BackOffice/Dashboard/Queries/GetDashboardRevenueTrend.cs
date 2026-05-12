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
public sealed record BackOfficeDashboardRevenueTrendPoint(DateOnly Month, decimal Revenue);

/// <summary>
///     Monthly ex-VAT revenue across every subscription's payment transactions, from the earliest non-refunded
///     transaction through the current month. Mirrors the Total Revenue KPI tile's metric — sum of
///     <see cref="PaymentTransaction.AmountExcludingTax" /> for succeeded transactions, refunded rows excluded.
///     Empty months between the earliest and current month appear with revenue zero so the line has no gaps.
///     Soft-delete semantic: historical revenue from soft-deleted tenants stays in the curve — payment
///     transactions are immutable historical money facts that outlive the tenant lifecycle, so a deleted
///     tenant must appear in months it was paying.
/// </summary>
public sealed class GetDashboardRevenueTrendHandler(ISubscriptionRepository subscriptionRepository, IPlatformCurrencyProvider platformCurrencyProvider, TimeProvider timeProvider)
    : IRequestHandler<GetDashboardRevenueTrendQuery, Result<BackOfficeDashboardRevenueTrendResponse>>
{
    public async Task<Result<BackOfficeDashboardRevenueTrendResponse>> Handle(GetDashboardRevenueTrendQuery query, CancellationToken cancellationToken)
    {
        var subscriptions = await subscriptionRepository.GetAllWithTransactionsUnfilteredAsync(cancellationToken);
        var transactions = subscriptions
            .SelectMany(s => s.PaymentTransactions)
            .Where(t => t.Status == PaymentTransactionStatus.Succeeded)
            .ToArray();

        if (transactions.Length == 0)
        {
            return new BackOfficeDashboardRevenueTrendResponse(platformCurrencyProvider.Currency, []);
        }

        var totalsByMonth = transactions
            .GroupBy(t => new DateOnly(t.Date.UtcDateTime.Year, t.Date.UtcDateTime.Month, 1))
            .ToDictionary(g => g.Key, g => g.Sum(t => t.AmountExcludingTax));

        var earliestMonth = totalsByMonth.Keys.Min();
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var currentMonth = new DateOnly(now.Year, now.Month, 1);

        var points = new List<BackOfficeDashboardRevenueTrendPoint>();
        for (var month = earliestMonth; month <= currentMonth; month = month.AddMonths(1))
        {
            points.Add(new BackOfficeDashboardRevenueTrendPoint(month, totalsByMonth.GetValueOrDefault(month, 0m)));
        }

        return new BackOfficeDashboardRevenueTrendResponse(platformCurrencyProvider.Currency, [.. points]);
    }
}
