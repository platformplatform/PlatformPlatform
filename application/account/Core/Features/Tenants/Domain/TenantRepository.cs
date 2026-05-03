using Account.Database;
using Account.Features.Subscriptions.Domain;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Domain;
using SharedKernel.ExecutionContext;
using SharedKernel.Persistence;

namespace Account.Features.Tenants.Domain;

public interface ITenantRepository : ICrudRepository<Tenant, TenantId>, ISoftDeletableRepository<Tenant, TenantId>
{
    Task<Tenant?> GetCurrentTenantAsync(CancellationToken cancellationToken);

    Task<bool> ExistsAsync(TenantId id, CancellationToken cancellationToken);

    Task<Tenant[]> GetByIdsAsync(TenantId[] ids, CancellationToken cancellationToken);

    /// <summary>
    ///     Retrieves a tenant by ID without applying tenant query filters.
    ///     This method should only be used in webhook processing where tenant context is not established.
    /// </summary>
    Task<Tenant?> GetByIdUnfilteredAsync(TenantId id, CancellationToken cancellationToken);

    Task<Tenant[]> SearchAllTenantsAsync(string? search, SubscriptionPlan[] plans, CancellationToken cancellationToken);
}

internal sealed class TenantRepository(AccountDbContext accountDbContext, IExecutionContext executionContext)
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
    ///     Retrieves a tenant by ID without applying tenant query filters.
    ///     This method should only be used in webhook processing where tenant context is not established.
    /// </summary>
    public async Task<Tenant?> GetByIdUnfilteredAsync(TenantId id, CancellationToken cancellationToken)
    {
        return await DbSet.IgnoreQueryFilters().SingleOrDefaultAsync(t => t.Id == id, cancellationToken);
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
}
