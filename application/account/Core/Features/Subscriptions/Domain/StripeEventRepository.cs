using Microsoft.EntityFrameworkCore;
using PlatformPlatform.Account.Database;
using PlatformPlatform.SharedKernel.Domain;
using PlatformPlatform.SharedKernel.Persistence;

namespace PlatformPlatform.Account.Features.Subscriptions.Domain;

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
}
