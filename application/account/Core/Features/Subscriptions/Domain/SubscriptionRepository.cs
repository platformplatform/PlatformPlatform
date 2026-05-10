using Account.Database;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Domain;
using SharedKernel.EntityFramework;
using SharedKernel.ExecutionContext;
using SharedKernel.Persistence;

namespace Account.Features.Subscriptions.Domain;

public interface ISubscriptionRepository : ICrudRepository<Subscription, SubscriptionId>
{
    Task<Subscription> GetCurrentAsync(CancellationToken cancellationToken);

    /// <summary>
    ///     Retrieves a subscription by Stripe customer ID with pessimistic locking (FOR UPDATE).
    ///     This method should only be used in webhook processing to serialize with user-action commands.
    ///     This method bypasses tenant query filters since webhooks have no tenant context.
    /// </summary>
    Task<Subscription?> GetByStripeCustomerIdWithLockUnfilteredAsync(StripeCustomerId stripeCustomerId, CancellationToken cancellationToken);

    /// <summary>
    ///     Retrieves a subscription by tenant ID without applying tenant query filters.
    ///     This method is used when tenant context is not available (e.g., during signup token creation).
    /// </summary>
    Task<Subscription?> GetByTenantIdUnfilteredAsync(TenantId tenantId, CancellationToken cancellationToken);

    /// <summary>
    ///     Retrieves all subscriptions for the given tenant ids without applying tenant query filters.
    ///     This method is used by back-office cross-tenant queries where tenant context is not established.
    /// </summary>
    Task<Subscription[]> GetByTenantIdsUnfilteredAsync(TenantId[] tenantIds, CancellationToken cancellationToken);

    /// <summary>
    ///     Retrieves every subscription on a paid plan (Plan != Basis) without applying tenant query filters.
    ///     Used by the back-office dashboard KPI snapshot to compute total monthly recurring revenue across all tenants.
    /// </summary>
    Task<Subscription[]> GetAllActiveUnfilteredAsync(CancellationToken cancellationToken);

    /// <summary>
    ///     Retrieves every subscription that has at least one payment transaction recorded. Used by the
    ///     back-office invoices listing which expands the JSON transactions array into one row per invoice.
    ///     Bypasses the tenant query filter because the back-office is cross-tenant by design.
    /// </summary>
    Task<Subscription[]> GetAllWithTransactionsUnfilteredAsync(CancellationToken cancellationToken);

    /// <summary>
    ///     Counts subscriptions where billing drift has been detected and not yet acknowledged. Bypasses the
    ///     tenant query filter because the back-office is cross-tenant by design.
    /// </summary>
    Task<int> CountWithDriftDetectedUnfilteredAsync(CancellationToken cancellationToken);

    /// <summary>
    ///     Counts paid subscriptions that have no rows in billing_events — i.e. subscriptions that have
    ///     never been synced into the BillingEvent log. The dashboard's MRR trend silently under-counts
    ///     these, so the back-office surfaces the count as a banner. Bypasses the tenant query filter
    ///     because the back-office is cross-tenant by design.
    /// </summary>
    Task<int> CountWithoutBillingEventsUnfilteredAsync(CancellationToken cancellationToken);
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
    ///     Retrieves a subscription by Stripe customer ID with pessimistic locking (FOR UPDATE).
    ///     This method should only be used in webhook processing to serialize with user-action commands.
    ///     This method bypasses tenant query filters since webhooks have no tenant context.
    /// </summary>
    public async Task<Subscription?> GetByStripeCustomerIdWithLockUnfilteredAsync(StripeCustomerId stripeCustomerId, CancellationToken cancellationToken)
    {
        if (accountDbContext.Database.ProviderName is "Microsoft.EntityFrameworkCore.Sqlite")
        {
            return await DbSet.IgnoreQueryFilters([QueryFilterNames.Tenant]).SingleOrDefaultAsync(s => s.StripeCustomerId == stripeCustomerId, cancellationToken);
        }

        return await DbSet
            .FromSqlInterpolated($"SELECT * FROM subscriptions WHERE stripe_customer_id = {stripeCustomerId.Value} FOR UPDATE")
            .IgnoreQueryFilters([QueryFilterNames.Tenant])
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<Subscription?> GetByTenantIdUnfilteredAsync(TenantId tenantId, CancellationToken cancellationToken)
    {
        return DbSet.Local.SingleOrDefault(s => s.TenantId == tenantId)
               ?? await DbSet.IgnoreQueryFilters([QueryFilterNames.Tenant]).SingleOrDefaultAsync(s => s.TenantId == tenantId, cancellationToken);
    }

    /// <summary>
    ///     Retrieves all subscriptions for the given tenant ids without applying tenant query filters.
    ///     This method is used by back-office cross-tenant queries where tenant context is not established.
    /// </summary>
    public async Task<Subscription[]> GetByTenantIdsUnfilteredAsync(TenantId[] tenantIds, CancellationToken cancellationToken)
    {
        return await DbSet.IgnoreQueryFilters([QueryFilterNames.Tenant]).Where(s => tenantIds.AsEnumerable().Contains(s.TenantId)).ToArrayAsync(cancellationToken);
    }

    /// <summary>
    ///     Retrieves every subscription on a paid plan (Plan != Basis) without applying tenant query filters.
    ///     Used by the back-office dashboard KPI snapshot to compute total monthly recurring revenue across all tenants.
    /// </summary>
    public async Task<Subscription[]> GetAllActiveUnfilteredAsync(CancellationToken cancellationToken)
    {
        return await DbSet.IgnoreQueryFilters([QueryFilterNames.Tenant]).Where(s => s.Plan != SubscriptionPlan.Basis).ToArrayAsync(cancellationToken);
    }

    public async Task<Subscription[]> GetAllWithTransactionsUnfilteredAsync(CancellationToken cancellationToken)
    {
        // PaymentTransactions is a jsonb column whose Length cannot be translated to SQL by EF Core
        // uniformly across providers (SQLite is used in tests, Postgres in dev/prod), so the materialized
        // set is filtered in memory. This is acceptable because back-office is the only caller and the row
        // count of subscriptions is small relative to other cross-tenant queries already done here.
        var subscriptions = await DbSet.IgnoreQueryFilters([QueryFilterNames.Tenant]).ToArrayAsync(cancellationToken);
        return [.. subscriptions.Where(s => s.PaymentTransactions.Length > 0)];
    }

    public async Task<int> CountWithDriftDetectedUnfilteredAsync(CancellationToken cancellationToken)
    {
        return await DbSet.IgnoreQueryFilters([QueryFilterNames.Tenant]).CountAsync(s => s.HasDriftDetected, cancellationToken);
    }

    public async Task<int> CountWithoutBillingEventsUnfilteredAsync(CancellationToken cancellationToken)
    {
        var tenantFilterName = new[] { QueryFilterNames.Tenant };
        return await DbSet.IgnoreQueryFilters(tenantFilterName)
            .Where(s => s.CurrentPriceAmount != null)
            .Where(s => !accountDbContext.Set<BillingEvent>().IgnoreQueryFilters(tenantFilterName).Any(e => e.SubscriptionId == s.Id))
            .CountAsync(cancellationToken);
    }
}
