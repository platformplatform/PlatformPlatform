using PlatformPlatform.AccountManagement.Api.Tenants.Domain;
using PlatformPlatform.SharedKernel.Domain.Entities;
using PlatformPlatform.SharedKernel.Infrastructure.Persistence;

namespace PlatformPlatform.AccountManagement.Api.AccountRegistrations.Domain;

public sealed class AccountRegistrationRepository(AccountManagementDbContext accountManagementDbContext)
    : RepositoryBase<AccountRegistration, AccountRegistrationId>(accountManagementDbContext), ICrudRepository<AccountRegistration, AccountRegistrationId>
{
    public AccountRegistration[] GetByEmailOrTenantId(TenantId tenantId, string email)
    {
        return accountManagementDbContext.AccountRegistrations
            .Where(r => !r.Completed)
            .Where(r => r.TenantId == tenantId || r.Email == email.ToLowerInvariant())
            .ToArray();
    }
}
