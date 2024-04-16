using PlatformPlatform.SharedKernel.DomainCore.Entities;

namespace PlatformPlatform.AccountManagement.Domain.AccountRegistrations;

public interface IAccountRegistrationRepository : ICrudRepository<AccountRegistration, AccountRegistrationId>
{
    AccountRegistration[] GetByEmailOrTenantId(TenantId tenantId, string email);
}
