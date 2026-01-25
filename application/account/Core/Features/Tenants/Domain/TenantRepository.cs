using Microsoft.EntityFrameworkCore;
using PlatformPlatform.Account.Database;
using PlatformPlatform.SharedKernel.Domain;
using PlatformPlatform.SharedKernel.ExecutionContext;
using PlatformPlatform.SharedKernel.Persistence;

namespace PlatformPlatform.Account.Features.Tenants.Domain;

public interface ITenantRepository : ICrudRepository<Tenant, TenantId>, ISoftDeletableRepository<Tenant, TenantId>
{
    Task<Tenant?> GetCurrentTenantAsync(CancellationToken cancellationToken);

    Task<bool> ExistsAsync(TenantId id, CancellationToken cancellationToken);

    Task<Tenant[]> GetByIdsAsync(TenantId[] ids, CancellationToken cancellationToken);
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
}
