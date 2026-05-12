using Account.Features.Subscriptions.Domain;

namespace Account.Features.BackOffice.Dashboard.Queries;

/// <summary>
///     Reconstructs MRR on a given date from the <see cref="BillingEvent" /> log: for each subscription,
///     the most recent event with NewAmount set (and OccurredAt before end-of-day on the date) is its
///     committed MRR for that day. Shared between <see cref="GetDashboardMrrTrendHandler" /> (which uses
///     it to build the daily series) and <see cref="GetDashboardKpisHandler" /> (which uses it to compute
///     the period-over-period delta against the start-of-window value).
/// </summary>
internal static class DashboardMrrCalculator
{
    public static decimal ComputeMrrOnDate(Dictionary<SubscriptionId, BillingEvent[]> eventsBySubscription, DateOnly date)
    {
        var endOfDay = new DateTimeOffset(date.AddDays(1).ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        var total = 0m;
        foreach (var subscriptionEvents in eventsBySubscription.Values)
        {
            var latest = subscriptionEvents.LastOrDefault(e => e.OccurredAt < endOfDay);
            if (latest?.NewAmount is { } amount) total += amount;
        }

        return total;
    }

    public static Dictionary<SubscriptionId, BillingEvent[]> GroupByOccurredAt(BillingEvent[] events)
    {
        return events
            .GroupBy(e => e.SubscriptionId)
            .ToDictionary(g => g.Key, g => g.OrderBy(e => e.OccurredAt).ToArray());
    }
}
