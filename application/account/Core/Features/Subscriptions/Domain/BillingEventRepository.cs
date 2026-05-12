using Account.Database;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Domain;
using SharedKernel.EntityFramework;
using SharedKernel.Persistence;

namespace Account.Features.Subscriptions.Domain;

public interface IBillingEventRepository : IAppendRepository<BillingEvent, BillingEventId>
{
    /// <summary>
    ///     Returns every billing event for a subscription. Used by drift detection and projection logic
    ///     that walks subscription history. Bypasses the tenant query filter because the drift detector
    ///     and webhook pipeline both run without an authenticated tenant context.
    /// </summary>
    Task<BillingEvent[]> GetBySubscriptionIdUnfilteredAsync(SubscriptionId subscriptionId, CancellationToken cancellationToken);

    /// <summary>
    ///     Returns the set of Stripe event ids already recorded for a subscription. Used to enforce the
    ///     1:1 invariant idempotently — a redelivered webhook or a re-pull from the Stripe events API
    ///     skips events whose ids are already in this set. Bypasses the tenant query filter because the
    ///     webhook pipeline runs without an authenticated tenant context.
    /// </summary>
    Task<HashSet<string>> GetExistingStripeEventIdsUnfilteredAsync(SubscriptionId subscriptionId, CancellationToken cancellationToken);

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

    /// <summary>
    ///     Returns every billing event with a non-null AmountDelta across all tenants — the events that
    ///     actually move committed MRR. Used by the dashboard MRR-trend computation. Bypasses the tenant
    ///     query filter because the back-office is cross-tenant by design.
    /// </summary>
    Task<BillingEvent[]> GetMrrChangeEventsUnfilteredAsync(CancellationToken cancellationToken);

    /// <summary>
    ///     Returns the subset of <paramref name="subscriptionIds" /> that have at least one billing event
    ///     recorded. Used by the back-office accounts list to filter to "unsynced" subscriptions (paid
    ///     subscriptions with no events). Bypasses the tenant query filter because the back-office is
    ///     cross-tenant by design.
    /// </summary>
    Task<HashSet<SubscriptionId>> GetSubscriptionIdsWithEventsUnfilteredAsync(SubscriptionId[] subscriptionIds, CancellationToken cancellationToken);
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

    public async Task<HashSet<string>> GetExistingStripeEventIdsUnfilteredAsync(SubscriptionId subscriptionId, CancellationToken cancellationToken)
    {
        var ids = await DbSet
            .IgnoreQueryFilters([QueryFilterNames.Tenant])
            .Where(e => e.SubscriptionId == subscriptionId)
            .Select(e => e.StripeEventId)
            .ToArrayAsync(cancellationToken);
        return [.. ids];
    }

    public async Task<BillingEvent[]> GetRecentUnfilteredAsync(int limit, CancellationToken cancellationToken)
    {
        // SQLite (used in tests) cannot translate DateTimeOffset comparisons in ORDER BY, so the sort runs
        // in memory. The materialized set is bounded by the dashboard's small request limit (max 50 rows).
        // NoOp rows are audit-only and hidden from the timeline display.
        var events = await DbSet
            .IgnoreQueryFilters([QueryFilterNames.Tenant])
            .Where(e => e.EventType != BillingEventType.NoOp)
            .ToArrayAsync(cancellationToken);
        return events.OrderByDescending(e => e.OccurredAt).Take(limit).ToArray();
    }

    public async Task<BillingEvent[]> GetMrrChangeEventsUnfilteredAsync(CancellationToken cancellationToken)
    {
        return await DbSet
            .IgnoreQueryFilters([QueryFilterNames.Tenant])
            .Where(e => e.AmountDelta != null)
            .ToArrayAsync(cancellationToken);
    }

    public async Task<HashSet<SubscriptionId>> GetSubscriptionIdsWithEventsUnfilteredAsync(SubscriptionId[] subscriptionIds, CancellationToken cancellationToken)
    {
        if (subscriptionIds.Length == 0) return [];

        var ids = await DbSet
            .IgnoreQueryFilters([QueryFilterNames.Tenant])
            .Where(e => subscriptionIds.AsEnumerable().Contains(e.SubscriptionId))
            .Select(e => e.SubscriptionId)
            .Distinct()
            .ToArrayAsync(cancellationToken);
        return [.. ids];
    }

    public async Task<BillingEvent[]> SearchAllUnfilteredAsync(BillingEventType[] eventTypes, DateTimeOffset? occurredFrom, DateTimeOffset? occurredTo, CancellationToken cancellationToken)
    {
        // NoOp rows are audit-only — hidden from the timeline display unless an admin explicitly filters
        // for them via the eventTypes parameter.
        var queryable = eventTypes.Length > 0
            ? DbSet.IgnoreQueryFilters([QueryFilterNames.Tenant]).Where(e => eventTypes.AsEnumerable().Contains(e.EventType))
            : DbSet.IgnoreQueryFilters([QueryFilterNames.Tenant]).Where(e => e.EventType != BillingEventType.NoOp);

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
