using Microsoft.EntityFrameworkCore;
using PlatformPlatform.AccountManagement.Database;
using PlatformPlatform.SharedKernel.Domain;
using PlatformPlatform.SharedKernel.ExecutionContext;
using PlatformPlatform.SharedKernel.Persistence;

namespace PlatformPlatform.AccountManagement.Features.Tenants.Domain;

public interface ITenantRepository : ICrudRepository<Tenant, TenantId>
{
    Task<Tenant> GetCurrentTenantAsync(CancellationToken cancellationToken);

    Task<bool> ExistsAsync(TenantId id, CancellationToken cancellationToken);

    Task<Tenant[]> GetByIdsAsync(TenantId[] ids, CancellationToken cancellationToken);

    /// <summary>
    ///     Retrieves a tenant by ID without applying tenant query filters.
    ///     This method should only be used in webhook processing where tenant context is not established.
    /// </summary>
    Task<Tenant?> GetByIdUnfilteredAsync(TenantId id, CancellationToken cancellationToken);
}

internal sealed class TenantRepository(AccountManagementDbContext accountManagementDbContext, IExecutionContext executionContext)
    : RepositoryBase<Tenant, TenantId>(accountManagementDbContext), ITenantRepository
{
    public async Task<Tenant> GetCurrentTenantAsync(CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(executionContext.TenantId!);
        return await GetByIdAsync(executionContext.TenantId, cancellationToken) ??
               throw new InvalidOperationException("Active tenant not found.");
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
        return await DbSet.IgnoreQueryFilters().FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
    }
}
