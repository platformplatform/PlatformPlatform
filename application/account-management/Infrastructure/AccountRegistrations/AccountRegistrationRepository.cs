using PlatformPlatform.AccountManagement.Domain.AccountRegistrations;
using PlatformPlatform.SharedKernel.InfrastructureCore.Persistence;

namespace PlatformPlatform.AccountManagement.Infrastructure.AccountRegistrations;

[UsedImplicitly]
public sealed class AccountRegistrationRepository(AccountManagementDbContext accountManagementDbContext)
    : RepositoryBase<AccountRegistration, AccountRegistrationId>(accountManagementDbContext),
        IAccountRegistrationRepository
{
    public AccountRegistration[] GetByEmailOrTenantId(TenantId tenantId, string email)
    {
        return accountManagementDbContext.AccountRegistrations
            .Where(r => !r.Completed)
            .Where(r => r.TenantId == tenantId || r.Email == email.ToLowerInvariant())
            .ToArray();
    }
}