using PlatformPlatform.SharedKernel.DomainCore.Entities;

namespace PlatformPlatform.AccountManagement.Domain.Tenants;

public interface ITenantRepository : ICrudRepository<Tenant, TenantId>
{
    Task<bool> ExistsAsync(TenantId id, CancellationToken cancellationToken);
    
    Task<bool> IsSubdomainFreeAsync(string subdomain, CancellationToken cancellationToken);
}
