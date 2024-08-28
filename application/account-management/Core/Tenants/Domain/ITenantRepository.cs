using PlatformPlatform.SharedKernel.Entities;

namespace PlatformPlatform.AccountManagement.Core.Tenants.Domain;

public interface ITenantRepository : ICrudRepository<Tenant, TenantId>
{
    Task<bool> ExistsAsync(TenantId id, CancellationToken cancellationToken);

    Task<bool> IsSubdomainFreeAsync(string subdomain, CancellationToken cancellationToken);
}
