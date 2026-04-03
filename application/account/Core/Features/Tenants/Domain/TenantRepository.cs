using Account.Database;
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

    /// <summary>
    ///     Retrieves all active tenants without applying tenant query filters.
    ///     This method should only be used by internal API endpoints where tenant context is not established.
    /// </summary>
    Task<Tenant[]> GetAllUnfilteredAsync(CancellationToken cancellationToken);

    Task<int> GetFeatureFlagVersionAsync(TenantId tenantId, CancellationToken cancellationToken);

    Task IncrementAllFeatureFlagVersionsAsync(CancellationToken cancellationToken);
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

    /// <summary>
    ///     Retrieves all active tenants without applying tenant query filters.
    ///     This method should only be used by internal API endpoints where tenant context is not established.
    /// </summary>
    public async Task<Tenant[]> GetAllUnfilteredAsync(CancellationToken cancellationToken)
    {
        return await DbSet.IgnoreQueryFilters().OrderBy(t => t.Id).ToArrayAsync(cancellationToken);
    }

    public async Task<int> GetFeatureFlagVersionAsync(TenantId tenantId, CancellationToken cancellationToken)
    {
        return await DbSet.Where(t => t.Id == tenantId).Select(t => t.FeatureFlagVersion).SingleOrDefaultAsync(cancellationToken);
    }

    public async Task IncrementAllFeatureFlagVersionsAsync(CancellationToken cancellationToken)
    {
        await accountDbContext.Database.ExecuteSqlRawAsync("UPDATE tenants SET feature_flag_version = feature_flag_version + 1", cancellationToken);
    }
}
