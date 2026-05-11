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

    Task<StripeEvent[]> GetPendingByStripeCustomerIdAsync(StripeCustomerId stripeCustomerId, CancellationToken cancellationToken);

    /// <summary>
    ///     Checks if any pending events exist for a Stripe customer without locking.
    ///     Used by the frontend to poll for webhook processing completion.
    /// </summary>
    Task<bool> HasPendingByStripeCustomerIdAsync(StripeCustomerId stripeCustomerId, CancellationToken cancellationToken);

    /// <summary>
    ///     Returns the durable-archive stripe_event rows for a customer that the replayer should
    ///     consume to (re)build the BillingEvent log. Includes both webhook-delivered and
    ///     reconciliation-recovered events (Status=Processed); excludes Pending (not yet
    ///     processed), Ignored (no customer match), and Failed. Source of truth for replay:
    ///     this archive, not Stripe's events.list (which only retains 30 days per
    ///     https://docs.stripe.com/api/events).
    /// </summary>
    Task<StripeEvent[]> GetReplayableByStripeCustomerIdAsync(StripeCustomerId stripeCustomerId, CancellationToken cancellationToken);

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
    ///     tenant context.
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

    public async Task<StripeEvent[]> GetPendingByStripeCustomerIdAsync(StripeCustomerId stripeCustomerId, CancellationToken cancellationToken)
    {
        return await DbSet
            .Where(e => e.StripeCustomerId == stripeCustomerId && e.Status == StripeEventStatus.Pending)
            .ToArrayAsync(cancellationToken);
    }

    public async Task<bool> HasPendingByStripeCustomerIdAsync(StripeCustomerId stripeCustomerId, CancellationToken cancellationToken)
    {
        return await DbSet.AnyAsync(e => e.StripeCustomerId == stripeCustomerId && e.Status == StripeEventStatus.Pending, cancellationToken);
    }

    public async Task<StripeEvent[]> GetReplayableByStripeCustomerIdAsync(StripeCustomerId stripeCustomerId, CancellationToken cancellationToken)
    {
        // No ORDER BY: the replayer is the canonical sort site (orders by CreatedAt then EventId for
        // a stable tie-break). Materializing here keeps the query SQLite-translatable and the set is
        // bounded per-customer (typically <200 webhooks over a subscription's lifetime). Status=Processed
        // covers both webhook-delivered events and reconciliation-recovered events (CreateRecovered lands
        // them as Processed directly).
        return await DbSet
            .Where(e => e.StripeCustomerId == stripeCustomerId && e.Status == StripeEventStatus.Processed)
            .ToArrayAsync(cancellationToken);
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

        var olderThanCutoff = candidateEvents
            .Where(e => e.StripeCreatedAt < cutoff)
            .OrderBy(e => e.StripeCreatedAt)
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
