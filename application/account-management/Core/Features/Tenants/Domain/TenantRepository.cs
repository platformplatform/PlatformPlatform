using PlatformPlatform.AccountManagement.Database;
using PlatformPlatform.SharedKernel.Domain;
using PlatformPlatform.SharedKernel.ExecutionContext;
using PlatformPlatform.SharedKernel.Persistence;

namespace PlatformPlatform.AccountManagement.Features.Tenants.Domain;

public interface ITenantRepository : ICrudRepository<Tenant, TenantId>
{
    Task<Tenant> GetCurrentTenantAsync(CancellationToken cancellationToken);

    Task<bool> ExistsAsync(TenantId id, CancellationToken cancellationToken);
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
}
