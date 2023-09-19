namespace PlatformPlatform.AccountManagement.Domain.Tenants;

public interface ITenantRepository
{
    Task<Tenant?> GetByIdAsync(TenantId id, CancellationToken cancellationToken);

    Task<bool> ExistsAsync(TenantId id, CancellationToken cancellationToken);

    Task AddAsync(Tenant aggregate, CancellationToken cancellationToken);

    void Update(Tenant aggregate);

    void Remove(Tenant aggregate);

    Task<bool> IsSubdomainFreeAsync(string subdomain, CancellationToken cancellationToken);
}