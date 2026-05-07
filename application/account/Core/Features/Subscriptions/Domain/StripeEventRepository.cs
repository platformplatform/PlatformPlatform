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
    ///     Returns previously-processed stripe_event rows for a customer ordered by CreatedAt.
    ///     Used by the legacy-data backfill to replay the customer's historical webhook chain
    ///     into the BillingEvent log. Pending events are excluded — they belong to the live
    ///     sync path which produces BillingEvents directly from state transitions.
    /// </summary>
    Task<StripeEvent[]> GetProcessedByStripeCustomerIdAsync(StripeCustomerId stripeCustomerId, CancellationToken cancellationToken);
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

    public async Task<StripeEvent[]> GetProcessedByStripeCustomerIdAsync(StripeCustomerId stripeCustomerId, CancellationToken cancellationToken)
    {
        // SQLite (used in tests) cannot translate DateTimeOffset comparisons in ORDER BY, so we order
        // in memory after materializing. The set is bounded per-customer (typically <200 webhooks).
        var events = await DbSet
            .Where(e => e.StripeCustomerId == stripeCustomerId && e.Status == StripeEventStatus.Processed)
            .ToArrayAsync(cancellationToken);
        return events.OrderBy(e => e.CreatedAt).ToArray();
    }
}
