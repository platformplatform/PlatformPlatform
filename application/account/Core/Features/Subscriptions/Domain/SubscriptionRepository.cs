using Microsoft.EntityFrameworkCore;
using PlatformPlatform.Account.Database;
using PlatformPlatform.SharedKernel.Domain;
using PlatformPlatform.SharedKernel.ExecutionContext;
using PlatformPlatform.SharedKernel.Persistence;

namespace PlatformPlatform.Account.Features.Subscriptions.Domain;

public interface ISubscriptionRepository : ICrudRepository<Subscription, SubscriptionId>
{
    Task<Subscription> GetCurrentAsync(CancellationToken cancellationToken);

    /// <summary>
    ///     Retrieves a subscription by Stripe customer ID with pessimistic locking (UPDLOCK).
    ///     This method should only be used in webhook processing to serialize with user-action commands.
    ///     This method bypasses tenant query filters since webhooks have no tenant context.
    /// </summary>
    Task<Subscription?> GetByStripeCustomerIdWithLockUnfilteredAsync(StripeCustomerId stripeCustomerId, CancellationToken cancellationToken);

    /// <summary>
    ///     Retrieves a subscription by tenant ID without applying tenant query filters.
    ///     This method is used when tenant context is not available (e.g., during signup token creation).
    /// </summary>
    Task<Subscription?> GetByTenantIdUnfilteredAsync(TenantId tenantId, CancellationToken cancellationToken);
}

internal sealed class SubscriptionRepository(AccountDbContext accountDbContext, IExecutionContext executionContext)
    : RepositoryBase<Subscription, SubscriptionId>(accountDbContext), ISubscriptionRepository
{
    public async Task<Subscription> GetCurrentAsync(CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(executionContext.TenantId);
        return await DbSet.SingleAsync(s => s.TenantId == executionContext.TenantId, cancellationToken);
    }

    /// <summary>
    ///     Retrieves a subscription by Stripe customer ID with pessimistic locking (UPDLOCK).
    ///     This method should only be used in webhook processing to serialize with user-action commands.
    ///     This method bypasses tenant query filters since webhooks have no tenant context.
    /// </summary>
    public async Task<Subscription?> GetByStripeCustomerIdWithLockUnfilteredAsync(StripeCustomerId stripeCustomerId, CancellationToken cancellationToken)
    {
        if (accountDbContext.Database.ProviderName == "Microsoft.EntityFrameworkCore.Sqlite")
        {
            return await DbSet.IgnoreQueryFilters().SingleOrDefaultAsync(s => s.StripeCustomerId == stripeCustomerId, cancellationToken);
        }

        return await DbSet
            .FromSqlInterpolated($"SELECT * FROM Subscriptions WITH (UPDLOCK, ROWLOCK) WHERE StripeCustomerId = {stripeCustomerId.Value}")
            .IgnoreQueryFilters()
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<Subscription?> GetByTenantIdUnfilteredAsync(TenantId tenantId, CancellationToken cancellationToken)
    {
        return await DbSet.IgnoreQueryFilters().SingleOrDefaultAsync(s => s.TenantId == tenantId, cancellationToken);
    }
}
