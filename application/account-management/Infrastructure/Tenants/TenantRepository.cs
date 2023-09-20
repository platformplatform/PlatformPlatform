using Microsoft.EntityFrameworkCore;
using PlatformPlatform.SharedKernel.InfrastructureCore.Persistence;

namespace PlatformPlatform.AccountManagement.Infrastructure.Tenants;

[UsedImplicitly]
internal sealed class TenantRepository : RepositoryBase<Tenant, TenantId>, ITenantRepository
{
    public TenantRepository(AccountManagementDbContext accountManagementDbContext) : base(accountManagementDbContext)
    {
    }

    public Task<bool> IsSubdomainFreeAsync(string subdomain, CancellationToken cancellationToken)
    {
        return DbSet.AllAsync(tenant => tenant.Id != subdomain, cancellationToken);
    }
}