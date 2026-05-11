using Account.Features.Subscriptions.Domain;
using Account.Integrations.Stripe;
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
    string? Currency,
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

/// <summary>
///     Reconstructs historical MRR from the <see cref="BillingEvent" /> log: the trend is the sum of each
///     subscription's latest <c>NewAmount</c> as-of each day in the window. This handler reads a different
///     writer than the dashboard KPI tile: the trend's source is the events.list writer (BillingEvent.NewAmount),
///     while the KPI reads the live Stripe-object writer (Subscription.ScheduledPriceAmount / current price).
///     The two writers run on different code paths and converge only after the BillingEvent is appended for a
///     given subscription change, so the <c>MrrMismatchBanner</c> may fire transiently during catalog edits
///     or while a Stripe event is in-flight. That is expected and self-heals once events are processed.
/// </summary>
public sealed class GetDashboardMrrTrendHandler(IBillingEventRepository billingEventRepository, IPlatformCurrencyProvider platformCurrencyProvider, TimeProvider timeProvider)
    : IRequestHandler<GetDashboardMrrTrendQuery, Result<BackOfficeDashboardMrrTrendResponse>>
{
    // Soft-delete semantic: every historical point sums the MRR from every subscription active at that time,
    // regardless of whether the tenant has since been soft-deleted. BillingEvent rows are immutable historical
    // money facts that outlive the tenant lifecycle, so a deleted tenant must appear in the historical curve at
    // the period it was paying — otherwise the trend silently rewrites the past every time a tenant churns.
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

        return new BackOfficeDashboardMrrTrendResponse(query.Period, platformCurrencyProvider.Currency, points, priorPoints);
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
