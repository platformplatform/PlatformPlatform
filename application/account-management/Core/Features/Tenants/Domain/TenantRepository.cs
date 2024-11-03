using Microsoft.EntityFrameworkCore;
using PlatformPlatform.AccountManagement.Database;
using PlatformPlatform.SharedKernel.Domain;
using PlatformPlatform.SharedKernel.Persistence;

namespace PlatformPlatform.AccountManagement.Features.Tenants.Domain;

public interface ITenantRepository : ICrudRepository<Tenant, TenantId>
{
    Task<bool> ExistsAsync(TenantId id, CancellationToken cancellationToken);

    Task<bool> IsSubdomainFreeAsync(string subdomain, CancellationToken cancellationToken);
}

internal sealed class TenantRepository(AccountManagementDbContext accountManagementDbContext)
    : RepositoryBase<Tenant, TenantId>(accountManagementDbContext), ITenantRepository
{
    public Task<bool> IsSubdomainFreeAsync(string subdomain, CancellationToken cancellationToken)
    {
        return DbSet.AllAsync(tenant => tenant.Id != subdomain, cancellationToken);
    }
}
