using PlatformPlatform.AccountManagement.Api.Tenants.Domain;
using PlatformPlatform.SharedKernel.DomainCore.Entities;

namespace PlatformPlatform.AccountManagement.Api.AccountRegistrations.Domain;

public interface IAccountRegistrationRepository : ICrudRepository<AccountRegistration, AccountRegistrationId>
{
    AccountRegistration[] GetByEmailOrTenantId(TenantId tenantId, string email);
}
