using System.Collections.Immutable;
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
    ///     Retrieves a subscription by Stripe customer ID without acquiring a row lock and without applying
    ///     tenant query filters. Used by the detect-only <c>BillingDriftWorker</c> tripwire: a Stripe roundtrip
    ///     under a <c>FOR UPDATE</c> lock would needlessly serialize the worker with the webhook hot path on
    ///     the same row for the full duration of the Stripe call. The result is returned untracked because
    ///     Detect mode never mutates the aggregate via the change tracker — drift status is written through
    ///     <see cref="UpdateDriftStatusAsync" /> as a column-only update.
    /// </summary>
    Task<Subscription?> GetByStripeCustomerIdUnfilteredAsync(StripeCustomerId stripeCustomerId, CancellationToken cancellationToken);

    /// <summary>
    ///     Persists the drift status fields (<c>has_drift_detected</c>, <c>drift_checked_at</c>,
    ///     <c>drift_discrepancies</c>) for a single subscription via a column-only UPDATE. Bypasses the
    ///     change tracker so the underlying row is never loaded under a <c>FOR UPDATE</c> lock first, which
    ///     keeps the detect-only <c>BillingDriftWorker</c> from blocking the webhook hot path on the same row.
    ///     Bypasses tenant query filters because the worker has no tenant context.
    /// </summary>
    Task UpdateDriftStatusAsync(SubscriptionId subscriptionId, bool hasDriftDetected, DateTimeOffset driftCheckedAt, ImmutableArray<DriftDiscrepancy> driftDiscrepancies, CancellationToken cancellationToken);

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
    ///     Retrieves every subscription whose drift check is stale, regardless of plan, without applying tenant
    ///     query filters. A subscription is "stale" when (a) it has a Stripe customer id (we need one to compare
    ///     against Stripe at all), and (b) DriftCheckedAt is either NULL or older than the supplied cutoff.
    /// </summary>
    Task<Subscription[]> GetSubscriptionsDueForDriftCheckUnfilteredAsync(DateTimeOffset cutoff, CancellationToken cancellationToken);

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

public sealed class SubscriptionRepository(AccountDbContext accountDbContext, IExecutionContext executionContext)
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

    /// <summary>
    ///     Retrieves a subscription by Stripe customer ID without acquiring a row lock and without applying
    ///     tenant query filters. Used by the detect-only <c>BillingDriftWorker</c> tripwire: a Stripe roundtrip
    ///     under a <c>FOR UPDATE</c> lock would needlessly serialize the worker with the webhook hot path on
    ///     the same row for the full duration of the Stripe call. The result is returned untracked because
    ///     Detect mode never mutates the aggregate via the change tracker — drift status is written through
    ///     <see cref="UpdateDriftStatusAsync" /> as a column-only update.
    /// </summary>
    public async Task<Subscription?> GetByStripeCustomerIdUnfilteredAsync(StripeCustomerId stripeCustomerId, CancellationToken cancellationToken)
    {
        return await DbSet
            .AsNoTracking()
            .IgnoreQueryFilters([QueryFilterNames.Tenant])
            .SingleOrDefaultAsync(s => s.StripeCustomerId == stripeCustomerId, cancellationToken);
    }

    /// <summary>
    ///     Persists the drift status fields (<c>has_drift_detected</c>, <c>drift_checked_at</c>,
    ///     <c>drift_discrepancies</c>) for a single subscription via a column-only UPDATE. Bypasses the
    ///     change tracker so the underlying row is never loaded under a <c>FOR UPDATE</c> lock first, which
    ///     keeps the detect-only <c>BillingDriftWorker</c> from blocking the webhook hot path on the same row.
    ///     Bypasses tenant query filters because the worker has no tenant context.
    /// </summary>
    public async Task UpdateDriftStatusAsync(SubscriptionId subscriptionId, bool hasDriftDetected, DateTimeOffset driftCheckedAt, ImmutableArray<DriftDiscrepancy> driftDiscrepancies, CancellationToken cancellationToken)
    {
        await DbSet
            .IgnoreQueryFilters([QueryFilterNames.Tenant])
            .Where(s => s.Id == subscriptionId)
            .ExecuteUpdateAsync(s => s
                    .SetProperty(x => x.HasDriftDetected, hasDriftDetected)
                    .SetProperty(x => x.DriftCheckedAt, driftCheckedAt)
                    .SetProperty(x => x.DriftDiscrepancies, driftDiscrepancies),
                cancellationToken
            );
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

    /// <summary>
    ///     Retrieves every subscription whose drift check is stale, regardless of plan, without applying tenant
    ///     query filters. A subscription is "stale" when (a) it has a Stripe customer id (we need one to compare
    ///     against Stripe at all), and (b) DriftCheckedAt is either NULL or older than the supplied cutoff.
    /// </summary>
    public async Task<Subscription[]> GetSubscriptionsDueForDriftCheckUnfilteredAsync(DateTimeOffset cutoff, CancellationToken cancellationToken)
    {
        // Both predicates are evaluated in memory: StripeCustomerId is a value-object-typed nullable column
        // that EF Core does not translate against null directly, and the DateTimeOffset comparison on the
        // nullable DriftCheckedAt does not round-trip cleanly across SQLite (tests) and Postgres (production).
        // The row count of subscriptions across all tenants is small and the worker runs once per Container
        // App scale-up, so the in-memory filter is acceptable here. Matches the established pattern used by
        // every other CreatedAt/OccurredAt range query in this SCS (see BillingEventRepository,
        // EmailLoginRepository, ExternalLoginRepository, UserRepository).
        var allSubscriptions = await DbSet
            .IgnoreQueryFilters([QueryFilterNames.Tenant])
            .OrderBy(s => s.Id)
            .ToArrayAsync(cancellationToken);

        return [.. allSubscriptions.Where(s => s.StripeCustomerId is not null && (s.DriftCheckedAt is null || s.DriftCheckedAt < cutoff))];
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
