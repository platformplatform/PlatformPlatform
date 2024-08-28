using PlatformPlatform.AccountManagement.Core.Tenants.Domain;
using PlatformPlatform.SharedKernel.DomainCore.Entities;

namespace PlatformPlatform.AccountManagement.Core.Signups.Domain;

public interface ISignupRepository : ICrudRepository<Signup, SignupId>
{
    Signup[] GetByEmailOrTenantId(TenantId tenantId, string email);
}
