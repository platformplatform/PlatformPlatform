using Account.Features.Subscriptions.Domain;
using FluentValidation;
using JetBrains.Annotations;
using SharedKernel.Cqrs;

namespace Account.Features.BackOffice.Dashboard.Queries;

[PublicAPI]
public sealed record GetDashboardMrrTrendQuery(DashboardTrendPeriod Period = DashboardTrendPeriod.Last30Days)
    : IRequest<Result<BackOfficeDashboardMrrTrendResponse>>;

[PublicAPI]
public sealed record BackOfficeDashboardMrrTrendResponse(
    DashboardTrendPeriod Period,
    string Currency,
    BackOfficeDashboardMrrTrendPoint[] Points,
    BackOfficeDashboardMrrTrendPoint[] PriorPoints
);

[PublicAPI]
public sealed record BackOfficeDashboardMrrTrendPoint(DateOnly Date, decimal MonthlyRecurringRevenue);

public sealed class GetDashboardMrrTrendQueryValidator : AbstractValidator<GetDashboardMrrTrendQuery>
{
    public GetDashboardMrrTrendQueryValidator()
    {
        RuleFor(x => x.Period).Must(p => Enum.IsDefined(typeof(DashboardTrendPeriod), p)).WithMessage("Period must be one of Last7Days, Last30Days, or Last90Days.");
    }
}

public sealed class GetDashboardMrrTrendHandler(IBillingEventRepository billingEventRepository, TimeProvider timeProvider)
    : IRequestHandler<GetDashboardMrrTrendQuery, Result<BackOfficeDashboardMrrTrendResponse>>
{
    private const string DefaultCurrency = "DKK";

    public async Task<Result<BackOfficeDashboardMrrTrendResponse>> Handle(GetDashboardMrrTrendQuery query, CancellationToken cancellationToken)
    {
        var days = DashboardTrendPeriods.GetDays(query.Period);
        var now = timeProvider.GetUtcNow();
        var today = DateOnly.FromDateTime(now.UtcDateTime);
        var startDate = today.AddDays(-(days - 1));
        var priorStartDate = startDate.AddDays(-days);

        // Reconstruct historical MRR from the BillingEvent log: for each subscription, the most recent
        // event with NewAmount set (and OccurredAt before end-of-day) is its committed MRR for that day.
        var events = await billingEventRepository.GetMrrChangeEventsUnfilteredAsync(cancellationToken);
        var eventsBySubscription = events
            .GroupBy(e => e.SubscriptionId)
            .ToDictionary(g => g.Key, g => g.OrderBy(e => e.OccurredAt).ToArray());

        var points = new BackOfficeDashboardMrrTrendPoint[days];
        var priorPoints = new BackOfficeDashboardMrrTrendPoint[days];
        for (var index = 0; index < days; index++)
        {
            var currentDate = startDate.AddDays(index);
            var priorDate = priorStartDate.AddDays(index);
            points[index] = new BackOfficeDashboardMrrTrendPoint(currentDate, ComputeDailyMrr(eventsBySubscription, currentDate));
            priorPoints[index] = new BackOfficeDashboardMrrTrendPoint(priorDate, ComputeDailyMrr(eventsBySubscription, priorDate));
        }

        return new BackOfficeDashboardMrrTrendResponse(query.Period, DefaultCurrency, points, priorPoints);
    }

    private static decimal ComputeDailyMrr(Dictionary<SubscriptionId, BillingEvent[]> eventsBySubscription, DateOnly date)
    {
        var endOfDay = new DateTimeOffset(date.AddDays(1).ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        var total = 0m;
        foreach (var subscriptionEvents in eventsBySubscription.Values)
        {
            // Events are sorted by OccurredAt asc — LastOrDefault picks the latest event up to end-of-day.
            var latest = subscriptionEvents.LastOrDefault(e => e.OccurredAt < endOfDay);
            if (latest?.NewAmount is { } amount) total += amount;
        }

        return total;
    }
}
