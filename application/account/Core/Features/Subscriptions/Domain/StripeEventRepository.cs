using Account.Database;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Domain;
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
}

internal sealed class StripeEventRepository(AccountDbContext accountDbContext)
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
}
