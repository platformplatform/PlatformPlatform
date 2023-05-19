using PlatformPlatform.Foundation.DddCqrsFramework.Persistence;

namespace PlatformPlatform.AccountManagement.Domain.Tenants;

public interface ITenantRepository : IRepository<Tenant, TenantId>
{
    Task<bool> IsSubdomainFreeAsync(string subdomain, CancellationToken cancellationToken);
}