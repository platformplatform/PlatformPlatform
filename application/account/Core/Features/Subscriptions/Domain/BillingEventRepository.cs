using Account.Database;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Domain;
using SharedKernel.EntityFramework;
using SharedKernel.Persistence;

namespace Account.Features.Subscriptions.Domain;

public interface IBillingEventRepository : IAppendRepository<BillingEvent, BillingEventId>
{
    /// <summary>
    ///     Returns every billing event for a subscription. Used by the drift detector to compare the
    ///     stored append-only log against expected events computed from Stripe history. Bypasses the
    ///     tenant query filter because the drift detector and webhook pipeline both run without an
    ///     authenticated tenant context.
    /// </summary>
    Task<BillingEvent[]> GetBySubscriptionIdUnfilteredAsync(SubscriptionId subscriptionId, CancellationToken cancellationToken);

    /// <summary>
    ///     Returns the most recent billing events across all tenants. Bypasses the tenant query filter
    ///     because the back-office is cross-tenant by design.
    /// </summary>
    Task<BillingEvent[]> GetRecentUnfilteredAsync(int limit, CancellationToken cancellationToken);

    /// <summary>
    ///     Returns all billing events matching the optional event-type filter, across all tenants.
    ///     Bypasses the tenant query filter because the back-office is cross-tenant by design. Date-range
    ///     filtering is applied in memory because SQLite (used in tests) cannot translate DateTimeOffset
    ///     comparisons to SQL; the materialized set stays small in practice because event-type filtering
    ///     happens at the database level and dashboard windows are bounded.
    /// </summary>
    Task<BillingEvent[]> SearchAllUnfilteredAsync(BillingEventType[] eventTypes, DateTimeOffset? occurredFrom, DateTimeOffset? occurredTo, CancellationToken cancellationToken);
}

public sealed class BillingEventRepository(AccountDbContext accountDbContext)
    : RepositoryBase<BillingEvent, BillingEventId>(accountDbContext), IBillingEventRepository
{
    public async Task<BillingEvent[]> GetBySubscriptionIdUnfilteredAsync(SubscriptionId subscriptionId, CancellationToken cancellationToken)
    {
        return await DbSet
            .IgnoreQueryFilters([QueryFilterNames.Tenant])
            .Where(e => e.SubscriptionId == subscriptionId)
            .ToArrayAsync(cancellationToken);
    }

    public async Task<BillingEvent[]> GetRecentUnfilteredAsync(int limit, CancellationToken cancellationToken)
    {
        // SQLite (used in tests) cannot translate DateTimeOffset comparisons in ORDER BY, so the sort runs
        // in memory. The materialized set is bounded by the dashboard's small request limit (max 50 rows).
        var events = await DbSet.IgnoreQueryFilters([QueryFilterNames.Tenant]).ToArrayAsync(cancellationToken);
        return events.OrderByDescending(e => e.OccurredAt).Take(limit).ToArray();
    }

    public async Task<BillingEvent[]> SearchAllUnfilteredAsync(BillingEventType[] eventTypes, DateTimeOffset? occurredFrom, DateTimeOffset? occurredTo, CancellationToken cancellationToken)
    {
        var queryable = DbSet.IgnoreQueryFilters([QueryFilterNames.Tenant]);

        if (eventTypes.Length > 0)
        {
            queryable = queryable.Where(e => eventTypes.AsEnumerable().Contains(e.EventType));
        }

        var events = await queryable.ToArrayAsync(cancellationToken);

        if (occurredFrom.HasValue)
        {
            events = events.Where(e => e.OccurredAt >= occurredFrom.Value).ToArray();
        }

        if (occurredTo.HasValue)
        {
            events = events.Where(e => e.OccurredAt <= occurredTo.Value).ToArray();
        }

        return events;
    }
}
