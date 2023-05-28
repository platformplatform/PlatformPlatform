using Microsoft.EntityFrameworkCore;
using PlatformPlatform.AccountManagement.Domain.Tenants;
using PlatformPlatform.SharedKernel.InfrastructureCore.Persistence;

namespace PlatformPlatform.AccountManagement.Infrastructure.Tenants;

internal sealed class TenantRepository : RepositoryBase<Tenant, TenantId>, ITenantRepository
{
    public TenantRepository(AccountManagementDbContext accountManagementDbContext) : base(accountManagementDbContext)
    {
    }

    public Task<bool> IsSubdomainFreeAsync(string subdomain, CancellationToken cancellationToken)
    {
        return DbSet.AllAsync(tenant => tenant.Subdomain != subdomain, cancellationToken);
    }
}