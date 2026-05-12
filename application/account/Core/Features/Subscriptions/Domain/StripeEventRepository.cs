using Account.Database;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Domain;
using SharedKernel.EntityFramework;
using SharedKernel.Persistence;

namespace Account.Features.Subscriptions.Domain;

public interface IStripeEventRepository : IAppendRepository<StripeEvent, StripeEventId>
{
    Task<bool> ExistsAsync(string stripeEventId, CancellationToken cancellationToken);

    void Update(StripeEvent aggregate);

    /// <summary>
    ///     Checks if any pending events exist for a Stripe customer without locking.
    ///     Used by the frontend to poll for webhook processing completion.
    /// </summary>
    Task<bool> HasPendingByStripeCustomerIdAsync(StripeCustomerId stripeCustomerId, CancellationToken cancellationToken);

    /// <summary>
    ///     Transitions every Pending stripe_events row for a Stripe customer to Processed via a
    ///     column-only UPDATE, backfilling the resolved <c>tenant_id</c> and <c>stripe_subscription_id</c>
    ///     at the same moment. Does not materialize any row, so the durable <c>payload</c> column is
    ///     never read; this is the hot-path replacement for fetch-then-mark patterns that round-tripped
    ///     the jsonb archive bytes through the application. Covers both the just-acked event and any
    ///     accumulated Pending orphans from prior partial deliveries so the next sync starts clean.
    /// </summary>
    Task MarkPendingProcessedByStripeCustomerIdAsync(StripeCustomerId stripeCustomerId, DateTimeOffset processedAt, TenantId tenantId, StripeSubscriptionId? stripeSubscriptionId, CancellationToken cancellationToken);

    /// <summary>
    ///     Returns the set of Stripe event ids (regardless of Status) already recorded for a customer.
    ///     Used by the reconciliation passes to detect events that exist in Stripe but not in our
    ///     archive — those are inserted as recovered events with status=Processed.
    /// </summary>
    Task<HashSet<string>> GetExistingEventIdsByStripeCustomerIdAsync(StripeCustomerId stripeCustomerId, CancellationToken cancellationToken);

    /// <summary>
    ///     Returns archived stripe_events for a customer whose <c>StripeCreatedAt</c> is strictly older than
    ///     <paramref name="cutoff" /> and that have no matching <c>billing_events</c> row yet. Used by the
    ///     admin reconcile flow to surface payloads that fell out of Stripe's 30-day events.list retention
    ///     window so an operator can confirm replay before they are projected into billing_events. Excludes
    ///     Pending (not yet processed), Ignored (no customer match), and Failed; orders ASC by
    ///     <c>StripeCreatedAt</c> so the replayer state machine consumes them in the order Stripe produced
    ///     them. Bypasses the tenant query filter because reconciliation runs outside an authenticated
    ///     tenant context. <c>StripeCreatedAt</c> is nullable on the aggregate because legacy rows from
    ///     before the column existed have NULL there; rows with NULL <c>StripeCreatedAt</c> are excluded by
    ///     SQL semantics of the <c>&lt; cutoff</c> filter (NULL comparisons yield NULL, which is filtered
    ///     out) so every row returned from this method has a non-null <c>StripeCreatedAt</c>.
    /// </summary>
    Task<StripeEvent[]> GetArchivedEventsOlderThanAsync(StripeCustomerId stripeCustomerId, DateTimeOffset cutoff, CancellationToken cancellationToken);
}

public sealed class StripeEventRepository(AccountDbContext accountDbContext)
    : RepositoryBase<StripeEvent, StripeEventId>(accountDbContext), IStripeEventRepository
{
    public async Task<bool> ExistsAsync(string stripeEventId, CancellationToken cancellationToken)
    {
        var id = StripeEventId.NewId(stripeEventId);
        return await DbSet.AnyAsync(e => e.Id == id, cancellationToken);
    }

    public async Task<bool> HasPendingByStripeCustomerIdAsync(StripeCustomerId stripeCustomerId, CancellationToken cancellationToken)
    {
        return await DbSet.AnyAsync(e => e.StripeCustomerId == stripeCustomerId && e.Status == StripeEventStatus.Pending, cancellationToken);
    }

    public async Task MarkPendingProcessedByStripeCustomerIdAsync(StripeCustomerId stripeCustomerId, DateTimeOffset processedAt, TenantId tenantId, StripeSubscriptionId? stripeSubscriptionId, CancellationToken cancellationToken)
    {
        // ExecuteUpdateAsync rewrites only the state-machine columns; the durable payload jsonb column is
        // never read. Filters on stripe_customer_id and status=Pending so the transition is idempotent —
        // a concurrent request that already consumed the rows produces a no-op rather than a double-write.
        await DbSet
            .Where(e => e.StripeCustomerId == stripeCustomerId && e.Status == StripeEventStatus.Pending)
            .ExecuteUpdateAsync(e => e
                    .SetProperty(x => x.Status, StripeEventStatus.Processed)
                    .SetProperty(x => x.ProcessedAt, processedAt)
                    .SetProperty(x => x.TenantId, tenantId)
                    .SetProperty(x => x.StripeSubscriptionId, stripeSubscriptionId),
                cancellationToken
            );
    }

    public async Task<HashSet<string>> GetExistingEventIdsByStripeCustomerIdAsync(StripeCustomerId stripeCustomerId, CancellationToken cancellationToken)
    {
        var ids = await DbSet
            .Where(e => e.StripeCustomerId == stripeCustomerId)
            .Select(e => e.Id.Value)
            .ToArrayAsync(cancellationToken);
        return [.. ids];
    }

    public async Task<StripeEvent[]> GetArchivedEventsOlderThanAsync(StripeCustomerId stripeCustomerId, DateTimeOffset cutoff, CancellationToken cancellationToken)
    {
        // Materialize first then filter by stripe_event_id presence in billing_events in memory. SQLite (used
        // in tests) cannot translate the DateTimeOffset comparison plus the cross-table NotContains pattern
        // into a single SQL query, and the per-customer event set is bounded (typically <200 webhooks over a
        // subscription's lifetime). Status=Processed covers both webhook-delivered and reconciliation-recovered
        // events; Pending/Ignored/Failed are excluded so partial-state rows never reach the replayer.
        var candidateEvents = await DbSet
            .Where(e => e.StripeCustomerId == stripeCustomerId && e.Status == StripeEventStatus.Processed)
            .ToArrayAsync(cancellationToken);

        // StripeCreatedAt is nullable on the aggregate; the `< cutoff` filter excludes rows with NULL
        // (NULL comparisons yield false in LINQ-to-Objects after materialization, mirroring SQL's
        // NULL-tri-state semantics). Every row reaching the OrderBy therefore has a non-null value.
        var olderThanCutoff = candidateEvents
            .Where(e => e.StripeCreatedAt < cutoff)
            .OrderBy(e => e.StripeCreatedAt!.Value)
            .ToArray();

        if (olderThanCutoff.Length == 0) return [];

        var candidateEventIds = olderThanCutoff.Select(e => e.Id.Value).ToArray();
        var billingEventIds = await Context.Set<BillingEvent>()
            .IgnoreQueryFilters([QueryFilterNames.Tenant])
            .Where(b => candidateEventIds.AsEnumerable().Contains(b.StripeEventId))
            .Select(b => b.StripeEventId)
            .ToArrayAsync(cancellationToken);

        var billingEventIdSet = new HashSet<string>(billingEventIds);
        return olderThanCutoff.Where(e => !billingEventIdSet.Contains(e.Id.Value)).ToArray();
    }
}
