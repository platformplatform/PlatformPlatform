using Microsoft.EntityFrameworkCore;
using PlatformPlatform.SharedKernel.Domain.Entities;
using PlatformPlatform.SharedKernel.Infrastructure.Persistence;

namespace PlatformPlatform.AccountManagement.Api.Tenants.Domain;

public sealed class TenantRepository(AccountManagementDbContext accountManagementDbContext)
    : RepositoryBase<Tenant, TenantId>(accountManagementDbContext), ICrudRepository<Tenant, TenantId>
{
    public Task<bool> IsSubdomainFreeAsync(string subdomain, CancellationToken cancellationToken)
    {
        return DbSet.AllAsync(tenant => tenant.Id != subdomain, cancellationToken);
    }
}
