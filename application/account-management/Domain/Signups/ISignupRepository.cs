using PlatformPlatform.SharedKernel.DomainCore.Entities;

namespace PlatformPlatform.AccountManagement.Domain.Signups;

public interface ISignupRepository : ICrudRepository<Signup, SignupId>
{
    Signup[] GetByEmailOrTenantId(TenantId tenantId, string email);
}
