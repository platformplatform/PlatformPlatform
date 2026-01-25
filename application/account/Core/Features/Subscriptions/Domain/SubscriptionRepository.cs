using Microsoft.EntityFrameworkCore;
using PlatformPlatform.Account.Database;
using PlatformPlatform.SharedKernel.Domain;
using PlatformPlatform.SharedKernel.ExecutionContext;
using PlatformPlatform.SharedKernel.Persistence;

namespace PlatformPlatform.Account.Features.Subscriptions.Domain;

public interface ISubscriptionRepository : ICrudRepository<Subscription, SubscriptionId>
{
    Task<Subscription?> GetByTenantIdAsync(CancellationToken cancellationToken);

    /// <summary>
    ///     Retrieves a subscription by Stripe customer ID without applying tenant query filters.
    ///     This method should only be used in webhook processing where tenant context is not established.
    /// </summary>
    Task<Subscription?> GetByStripeCustomerIdUnfilteredAsync(string stripeCustomerId, CancellationToken cancellationToken);
}

internal sealed class SubscriptionRepository(AccountDbContext accountDbContext, IExecutionContext executionContext)
    : RepositoryBase<Subscription, SubscriptionId>(accountDbContext), ISubscriptionRepository
{
    public async Task<Subscription?> GetByTenantIdAsync(CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(executionContext.TenantId);
        return await DbSet.FirstOrDefaultAsync(s => s.TenantId == executionContext.TenantId, cancellationToken);
    }

    /// <summary>
    ///     Retrieves a subscription by Stripe customer ID without applying tenant query filters.
    ///     This method should only be used in webhook processing where tenant context is not established.
    /// </summary>
    public async Task<Subscription?> GetByStripeCustomerIdUnfilteredAsync(string stripeCustomerId, CancellationToken cancellationToken)
    {
        return await DbSet.IgnoreQueryFilters().FirstOrDefaultAsync(s => s.StripeCustomerId == stripeCustomerId, cancellationToken);
    }
}
