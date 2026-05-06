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
        return await DbSet
            .IgnoreQueryFilters([QueryFilterNames.Tenant])
            .OrderByDescending(e => e.OccurredAt)
            .Take(limit)
            .ToArrayAsync(cancellationToken);
    }
}
