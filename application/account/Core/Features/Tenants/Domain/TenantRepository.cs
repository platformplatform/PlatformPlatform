using Account.Database;
using Account.Features.Subscriptions.Domain;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Domain;
using SharedKernel.EntityFramework;
using SharedKernel.ExecutionContext;
using SharedKernel.Persistence;

namespace Account.Features.Tenants.Domain;

public interface ITenantRepository : ICrudRepository<Tenant, TenantId>, ISoftDeletableRepository<Tenant, TenantId>
{
    Task<Tenant?> GetCurrentTenantAsync(CancellationToken cancellationToken);

    Task<bool> ExistsAsync(TenantId id, CancellationToken cancellationToken);

    Task<Tenant[]> GetByIdsAsync(TenantId[] ids, CancellationToken cancellationToken);

    /// <summary>
    ///     Retrieves tenants by IDs without applying tenant query filters.
    ///     This method should only be used for internal back-office operations that need cross-tenant access.
    /// </summary>
    Task<Tenant[]> GetByIdsUnfilteredAsync(TenantId[] ids, CancellationToken cancellationToken);

    /// <summary>
    ///     Retrieves a tenant by ID without applying tenant query filters.
    ///     This method should only be used in webhook processing where tenant context is not established.
    /// </summary>
    Task<Tenant?> GetByIdUnfilteredAsync(TenantId id, CancellationToken cancellationToken);

    Task<Tenant[]> SearchAllTenantsAsync(string? search, SubscriptionPlan[] plans, CancellationToken cancellationToken);

    /// <summary>
    ///     Looks up tenant names for a set of tenant ids without applying tenant query filters.
    ///     This method is used by back-office cross-tenant queries that need to attach the tenant name
    ///     to a list of records (users, sessions, ...) where tenant context is not established.
    /// </summary>
    Task<Dictionary<TenantId, string>> GetNamesByIdsUnfilteredAsync(TenantId[] ids, CancellationToken cancellationToken);

    /// <summary>
    ///     Returns every tenant created at or after <paramref name="since" /> without applying tenant query filters.
    ///     Used by the back-office dashboard to compute new-tenant trend buckets across all tenants.
    /// </summary>
    Task<Tenant[]> GetCreatedSinceUnfilteredAsync(DateTimeOffset since, CancellationToken cancellationToken);

    /// <summary>
    ///     Returns every tenant without applying tenant query filters.
    ///     Used by the back-office dashboard KPI snapshot to count tenants by state and plan across all tenants.
    ///     Also used by internal API endpoints where tenant context is not established.
    /// </summary>
    Task<Tenant[]> GetAllUnfilteredAsync(CancellationToken cancellationToken);

    /// <summary>
    ///     Returns the <paramref name="limit" /> most recently created tenants without applying tenant query filters.
    ///     Used by the back-office dashboard "Recent signups" list.
    /// </summary>
    Task<Tenant[]> GetMostRecentSignupsUnfilteredAsync(int limit, CancellationToken cancellationToken);

    /// <summary>
    ///     Returns the next monotonic rollout index for a new tenant, used by the low-discrepancy bucket
    ///     function in <see cref="Tenant" /> to assign a well-spread rollout bucket. The Postgres sequence
    ///     guarantees uniqueness under concurrent CreateTenantCommand calls; SQLite (test database) falls
    ///     back to COUNT(*) which preserves the previous behavior.
    ///     Tenant query filter is ignored because tenant creation predates tenant context.
    /// </summary>
    Task<int> GetNextRolloutIndexUnfilteredAsync(CancellationToken cancellationToken);
}

public sealed class TenantRepository(AccountDbContext accountDbContext, IExecutionContext executionContext)
    : SoftDeletableRepositoryBase<Tenant, TenantId>(accountDbContext), ITenantRepository
{
    public async Task<Tenant?> GetCurrentTenantAsync(CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(executionContext.TenantId!);
        return await GetByIdAsync(executionContext.TenantId, cancellationToken);
    }

    public async Task<Tenant[]> GetByIdsAsync(TenantId[] ids, CancellationToken cancellationToken)
    {
        return await DbSet.Where(t => ids.AsEnumerable().Contains(t.Id)).ToArrayAsync(cancellationToken);
    }

    /// <summary>
    ///     Retrieves tenants by IDs without applying tenant query filters.
    ///     This method should only be used for internal back-office operations that need cross-tenant access.
    /// </summary>
    public async Task<Tenant[]> GetByIdsUnfilteredAsync(TenantId[] ids, CancellationToken cancellationToken)
    {
        if (ids.Length == 0) return [];

        return await DbSet.IgnoreQueryFilters([QueryFilterNames.Tenant]).Where(t => ids.AsEnumerable().Contains(t.Id)).ToArrayAsync(cancellationToken);
    }

    /// <summary>
    ///     Retrieves a tenant by ID without applying tenant query filters.
    ///     This method should only be used in webhook processing where tenant context is not established.
    /// </summary>
    public async Task<Tenant?> GetByIdUnfilteredAsync(TenantId id, CancellationToken cancellationToken)
    {
        return await DbSet.IgnoreQueryFilters([QueryFilterNames.Tenant]).SingleOrDefaultAsync(t => t.Id == id, cancellationToken);
    }

    public async Task<Tenant[]> SearchAllTenantsAsync(string? search, SubscriptionPlan[] plans, CancellationToken cancellationToken)
    {
        IQueryable<Tenant> tenants = DbSet;

        if (!string.IsNullOrWhiteSpace(search))
        {
            // TenantId is a long, so an exact match on a parsable id is the only way to filter by id at the DB level.
            // Partial id matches are not supported - operators search by tenant name for fuzzy matches.
            var idMatch = long.TryParse(search, out var parsedId) ? new TenantId(parsedId) : null;
            tenants = tenants.Where(t => t.Name.ToLower().Contains(search) || (idMatch != null && t.Id == idMatch));
        }

        if (plans.Length > 0)
        {
            tenants = tenants.Where(t => plans.AsEnumerable().Contains(t.Plan));
        }

        return await tenants.ToArrayAsync(cancellationToken);
    }

    /// <summary>
    ///     Looks up tenant names for a set of tenant ids without applying tenant query filters.
    ///     This method is used by back-office cross-tenant queries that need to attach the tenant name
    ///     to a list of records (users, sessions, ...) where tenant context is not established.
    /// </summary>
    public async Task<Dictionary<TenantId, string>> GetNamesByIdsUnfilteredAsync(TenantId[] ids, CancellationToken cancellationToken)
    {
        if (ids.Length == 0) return new Dictionary<TenantId, string>();

        return await DbSet
            .IgnoreQueryFilters([QueryFilterNames.Tenant])
            .Where(t => ids.AsEnumerable().Contains(t.Id))
            .ToDictionaryAsync(t => t.Id, t => t.Name, cancellationToken);
    }

    /// <summary>
    ///     Returns every tenant created at or after <paramref name="since" /> without applying tenant query filters.
    ///     Used by the back-office dashboard to compute new-tenant trend buckets across all tenants.
    ///     SQLite cannot translate DateTimeOffset comparisons in WHERE, so the time filter runs in memory; the
    ///     dashboard period is bounded (max 90 days) so the materialized set stays small.
    /// </summary>
    public async Task<Tenant[]> GetCreatedSinceUnfilteredAsync(DateTimeOffset since, CancellationToken cancellationToken)
    {
        var tenants = await DbSet.IgnoreQueryFilters([QueryFilterNames.Tenant]).ToArrayAsync(cancellationToken);
        return tenants.Where(t => t.CreatedAt >= since).ToArray();
    }

    /// <summary>
    ///     Returns every tenant without applying tenant query filters.
    ///     Used by the back-office dashboard KPI snapshot to count tenants by state and plan across all tenants.
    ///     Also used by internal API endpoints where tenant context is not established.
    /// </summary>
    public async Task<Tenant[]> GetAllUnfilteredAsync(CancellationToken cancellationToken)
    {
        return await DbSet.IgnoreQueryFilters([QueryFilterNames.Tenant]).OrderBy(t => t.Id).ToArrayAsync(cancellationToken);
    }

    /// <summary>
    ///     Returns the <paramref name="limit" /> most recently created tenants without applying tenant query filters.
    ///     Used by the back-office dashboard "Recent signups" list.
    ///     SQLite cannot translate DateTimeOffset comparisons, so the order-by runs in memory; the limit keeps the
    ///     materialized set small.
    /// </summary>
    public async Task<Tenant[]> GetMostRecentSignupsUnfilteredAsync(int limit, CancellationToken cancellationToken)
    {
        var tenants = await DbSet.IgnoreQueryFilters([QueryFilterNames.Tenant]).ToArrayAsync(cancellationToken);
        return tenants.OrderByDescending(t => t.CreatedAt).Take(limit).ToArray();
    }

    /// <summary>
    ///     Returns the next monotonic rollout index for a new tenant, used by the low-discrepancy bucket
    ///     function in <see cref="Tenant" /> to assign a well-spread rollout bucket. The Postgres sequence
    ///     guarantees uniqueness under concurrent CreateTenantCommand calls; SQLite (test database) falls
    ///     back to COUNT(*) which preserves the previous behavior.
    ///     Tenant query filter is ignored because tenant creation predates tenant context.
    /// </summary>
    public async Task<int> GetNextRolloutIndexUnfilteredAsync(CancellationToken cancellationToken)
    {
        if (accountDbContext.Database.ProviderName is "Microsoft.EntityFrameworkCore.Sqlite")
        {
            return await DbSet.IgnoreQueryFilters([QueryFilterNames.Tenant]).CountAsync(cancellationToken);
        }

        var nextSequenceValue = await accountDbContext.Database
            .SqlQueryRaw<long>("SELECT nextval('tenant_rollout_index_sequence') AS \"Value\"")
            .SingleAsync(cancellationToken);

        // Sequence is seeded at COUNT(*)+1, so the first nextval after migration equals the previous COUNT+1.
        // Subtracting 1 preserves the original count-based contract used by Tenant.Create. The mask keeps
        // the cast non-negative past 2.1B cumulative inserts (including rolled-back/leaked values) while
        // preserving int.MaxValue itself (the `% int.MaxValue` form silently collapses that value to 0).
        return (int)((nextSequenceValue - 1) & 0x7FFF_FFFF);
    }
}
