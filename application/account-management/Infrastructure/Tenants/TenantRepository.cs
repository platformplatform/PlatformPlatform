using Microsoft.EntityFrameworkCore;
using PlatformPlatform.SharedKernel.InfrastructureCore.Persistence;

namespace PlatformPlatform.AccountManagement.Infrastructure.Tenants;

[UsedImplicitly]
internal sealed class TenantRepository(AccountManagementDbContext accountManagementDbContext)
    : RepositoryBase<Tenant, TenantId>(accountManagementDbContext), ITenantRepository
{
    public Task<bool> IsSubdomainFreeAsync(string subdomain, CancellationToken cancellationToken)
    {
        return DbSet.AllAsync(tenant => tenant.Id != subdomain, cancellationToken);
    }
}